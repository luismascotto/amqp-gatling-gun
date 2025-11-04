namespace QueueProcessor.Worker.Configuration;

public enum OnCapacityBehavior
{
	Wait = 0,
	Accept = 1,
	Skip = 2
}

public sealed class QueueOptions
{
	public string? Provider { get; set; }
	public string? ConnectionString { get; set; }
	public string? QueueName { get; set; }
	public int MaxConcurrentHandlers { get; set; } = 1;
	public OnCapacityBehavior OnCapacity { get; set; } = OnCapacityBehavior.Wait;
	public bool EmitTestMessages { get; set; } = true;
	public int EmulatedMessageIntervalSeconds { get; set; } = 5;
}


