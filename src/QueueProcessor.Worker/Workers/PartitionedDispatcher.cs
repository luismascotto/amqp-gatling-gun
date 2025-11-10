using Microsoft.Extensions.Options;
using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Configuration;
using QueueProcessor.Worker.Infrastructure;
using QueueProcessor.Worker.Models;

namespace QueueProcessor.Worker.Workers;

public class PartitionedDispatcher : BackgroundService
{
	private readonly ILogger _logger;
	private readonly IMessageProcessor _processor;
	private readonly IMessageQueueClient _queueClient;
	private readonly PartitionedBuffer _buffer;
	private readonly BufferingOptions _options;

	private readonly object _inflightSync = new();
	private readonly Dictionary<int, int> _inflightPerTenant = new();

	public PartitionedDispatcher(
		ILogger logger,
		IMessageProcessor processor,
		IMessageQueueClient queueClient,
		PartitionedBuffer buffer,
		IOptions<BufferingOptions> options)
	{
		_logger = logger;
		_processor = processor;
		_queueClient = queueClient;
		_buffer = buffer;
		_options = options.Value;
		_options.MaxConcurrentHandlers = Math.Max(1, _options.MaxConcurrentHandlers);
		_options.MaxConcurrentHandlersPerTenant = Math.Max(1, _options.MaxConcurrentHandlersPerTenant);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("PartitionedDispatcher starting with overall={Overall} perTenant={PerTenant}", _options.MaxConcurrentHandlers, _options.MaxConcurrentHandlersPerTenant);
		var consumers = new List<Task>(_options.MaxConcurrentHandlers);
		for (int i = 0; i < _options.MaxConcurrentHandlers; i++)
		{
			consumers.Add(ConsumeLoopAsync(stoppingToken));
		}
		await Task.WhenAll(consumers);
		_logger.LogInformation("PartitionedDispatcher stopped.");
	}

	private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var message = await _buffer.DequeueNextEligibleAsync(IsTenantEligible, stoppingToken);
				IncrementInflight(message.TenantId);
				try
				{
					await _processor.ProcessAsync(message.Original, stoppingToken);
					await _queueClient.AcceptMessageAsync(message.Original, stoppingToken);
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error processing message {MessageId} for tenant {TenantId}", message.Original.Id, message.TenantId);
					try
					{
						await _queueClient.SkipMessageAsync(message.Original, stoppingToken);
					}
					catch (Exception inner)
					{
						_logger.LogError(inner, "Failed to skip message {MessageId}", message.Original.Id);
					}
				}
				finally
				{
					DecrementInflight(message.TenantId);
					_buffer.NotifyEligibilityChanged();
				}
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error in consumer loop");
				await Task.Delay(200, stoppingToken);
			}
		}
	}

	private bool IsTenantEligible(int tenantId)
	{
		lock (_inflightSync)
		{
			_inflightPerTenant.TryGetValue(tenantId, out var current);
			return current < _options.MaxConcurrentHandlersPerTenant;
		}
	}

	private void IncrementInflight(int tenantId)
	{
		lock (_inflightSync)
		{
			_inflightPerTenant.TryGetValue(tenantId, out var current);
			_inflightPerTenant[tenantId] = current + 1;
		}
	}

	private void DecrementInflight(int tenantId)
	{
		lock (_inflightSync)
		{
			if (_inflightPerTenant.TryGetValue(tenantId, out var current) && current > 0)
			{
				_inflightPerTenant[tenantId] = current - 1;
			}
			else
			{
				_inflightPerTenant[tenantId] = 0;
			}
		}
	}
}



