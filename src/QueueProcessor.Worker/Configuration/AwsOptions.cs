namespace QueueProcessor.Worker.Configuration;

public sealed class AwsOptions
{
	public bool UseDefaultCredentials { get; set; } = true;
	public string? AccessKeyId { get; set; }
	public string? SecretAccessKey { get; set; }
	public string? SessionToken { get; set; }
	public string? Region { get; set; }
	public string? ServiceURL { get; set; }
}


