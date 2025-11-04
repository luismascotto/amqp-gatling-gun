namespace QueueProcessor.Worker.Configuration;

public sealed class QueueOptions
{
	public string? Provider { get; set; }
	public string? ConnectionString { get; set; }
	public string? QueueName { get; set; }
	public int MaxConcurrentHandlers { get; set; } = 1;
	public bool EmitTestMessages { get; set; } = true;
	public int EmulatedMessageIntervalSeconds { get; set; } = 5;
}


