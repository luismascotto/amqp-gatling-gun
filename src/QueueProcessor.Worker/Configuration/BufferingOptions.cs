namespace QueueProcessor.Worker.Configuration;

public sealed class BufferingOptions
{
    public int MaxBufferedMessages { get; set; } = 64;
    public int MaxBufferedMessagesPerTenant { get; set; } = 4;
    public int MaxConcurrentHandlers { get; set; } = 4;
    public int MaxConcurrentHandlersPerTenant { get; set; } = 1;
    public OnCapacityBehavior OnTenantCapacity { get; set; } = OnCapacityBehavior.Wait;
}


