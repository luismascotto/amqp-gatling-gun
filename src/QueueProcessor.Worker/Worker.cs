namespace QueueProcessor.Worker;

using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Configuration;
using Microsoft.Extensions.Options;

public class Worker : BackgroundService
{
	private readonly ILogger<Worker> _logger;
	private readonly IMessageQueueClient _queueClient;
	private readonly IMessageProcessor _messageProcessor;
	private readonly QueueOptions _options;
	private readonly SemaphoreSlim _semaphore;

	public Worker(ILogger<Worker> logger, IMessageQueueClient queueClient, IMessageProcessor messageProcessor, IOptions<QueueOptions> options)
	{
		_logger = logger;
		_queueClient = queueClient;
		_messageProcessor = messageProcessor;
		_options = options.Value;
		int maxDegree = Math.Max(1, _options.MaxConcurrentHandlers);
		_semaphore = new SemaphoreSlim(maxDegree, maxDegree);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Message worker starting with concurrency {Max}", Math.Max(1, _options.MaxConcurrentHandlers));
		await foreach (var message in _queueClient.ReadMessagesAsync(stoppingToken))
		{
			if (_options.OnCapacity == OnCapacityBehavior.Wait)
			{
				await _semaphore.WaitAsync(stoppingToken);
				_ = ProcessMessageAsync(message, stoppingToken);
				continue;
			}

			if (_semaphore.Wait(0, CancellationToken.None))
			{
				_logger.LogInformation("Processing message {MessageId}.", message.Id);
				_ = ProcessMessageAsync(message, stoppingToken);
				continue;
			}

			_logger.LogWarning("At capacity. OnCapacity: {OnCapacity}", _options.OnCapacity);
			switch (_options.OnCapacity)
			{
				case OnCapacityBehavior.Accept:
					try
					{
						await _queueClient.AcceptMessageAsync(message, stoppingToken);
						_logger.LogWarning("Accepted message {MessageId} due to capacity.", message.Id);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to accept message {MessageId} when at capacity", message.Id);
					}
					break;
				case OnCapacityBehavior.Skip:
					try
					{
						await _queueClient.SkipMessageAsync(message, stoppingToken);
						_logger.LogWarning("Skipped message {MessageId} due to capacity.", message.Id);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to skip message {MessageId} when at capacity", message.Id);
					}
					break;
				default:
					// Fallback to waiting if an unknown enum value is supplied
					await _semaphore.WaitAsync(stoppingToken);
					_ = ProcessMessageAsync(message, stoppingToken);
					break;
			}
		}
		_logger.LogInformation("Message worker stopped.");
	}

	private async Task ProcessMessageAsync(QueueMessage message, CancellationToken stoppingToken)
	{
		try
		{
			await _messageProcessor.ProcessAsync(message, stoppingToken);
			await _queueClient.AcceptMessageAsync(message, stoppingToken);
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		{
			// graceful shutdown
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Processing failed for message {MessageId}", message.Id);
			try { await _queueClient.SkipMessageAsync(message, stoppingToken); }
			catch (Exception inner)
			{
				_logger.LogError(inner, "Failed to skip message {MessageId} after processing error", message.Id);
			}
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
