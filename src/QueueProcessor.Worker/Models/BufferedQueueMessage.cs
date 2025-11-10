using QueueProcessor.Worker.Abstractions;

namespace QueueProcessor.Worker.Models;

public sealed class BufferedQueueMessage
{
	public BufferedQueueMessage(int tenantId, QueueMessage original)
	{
		TenantId = tenantId;
		Original = original;
	}

	public int TenantId { get; }
	public QueueMessage Original { get; }
}


