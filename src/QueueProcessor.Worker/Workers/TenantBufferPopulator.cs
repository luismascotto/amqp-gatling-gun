using System.Text.Json;
using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Infrastructure;
using QueueProcessor.Worker.Models;

namespace QueueProcessor.Worker.Workers;

public sealed class TenantBufferPopulator : BackgroundService
{
	private readonly ILogger<TenantBufferPopulator> _logger;
	private readonly IMessageQueueClient _queueClient;
	private readonly TenantAwareBuffer _buffer;

	public TenantBufferPopulator(ILogger<TenantBufferPopulator> logger, IMessageQueueClient queueClient, TenantAwareBuffer buffer)
	{
		_logger = logger;
		_queueClient = queueClient;
		_buffer = buffer;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("TenantBufferPopulator starting...");
		await foreach (var msg in _queueClient.ReadMessagesAsync(stoppingToken))
		{
			int tenantId = ExtractTenantId(msg);
			var buffered = new BufferedQueueMessage(tenantId, msg);
			try
			{
				await _buffer.EnqueueAsync(buffered, stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to enqueue message {MessageId} for tenant {TenantId}", msg.Id, tenantId);
				// Intentionally do not Accept/Skip here; downstream worker will decide
			}
		}
		_logger.LogInformation("TenantBufferPopulator stopped.");
	}

	private static int ExtractTenantId(QueueMessage message)
	{
		try
		{
			using var doc = JsonDocument.Parse(message.Body);
			var root = doc.RootElement;
			if (TryGetInt(root, "Tenant_ID", out var id) ||
			    TryGetInt(root, "tenant_id", out id) ||
			    TryGetInt(root, "TenantId", out id) ||
			    TryGetInt(root, "tenantId", out id))
			{
				return id;
			}
		}
		catch
		{
			// Not JSON or no tenant id - fall through
		}
		return 0;
	}

	private static bool TryGetInt(JsonElement element, string propertyName, out int value)
	{
		value = 0;
		if (!element.TryGetProperty(propertyName, out var prop))
		{
			return false;
		}
		switch (prop.ValueKind)
		{
			case JsonValueKind.Number:
				return prop.TryGetInt32(out value);
			case JsonValueKind.String:
				return int.TryParse(prop.GetString(), out value);
			default:
				return false;
		}
	}
}


