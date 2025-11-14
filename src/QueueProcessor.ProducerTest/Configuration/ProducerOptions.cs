namespace QueueProcessor.ProducerTest.Configuration;

public sealed class ProducerOptions
{
	public int? Messages { get; set; }
	public int DelayMsMin { get; set; } = 50;
	public int DelayMsMax { get; set; } = 150;
	public int TenantMin { get; set; } = 1;
	public int TenantMax { get; set; } = 20;
}


