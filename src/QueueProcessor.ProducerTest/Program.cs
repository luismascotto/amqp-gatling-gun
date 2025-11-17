using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QueueProcessor.ProducerTest.Configuration;
using QueueProcessor.ProducerTest.Services;

// Load .env early so environment variables are available to the Host configuration
LoadDotEnv();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection("Aws"));
builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection("Queue"));
builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection("Producer"));

var user = builder.Configuration.GetSection("Aws").GetValue<string>("AccessKeyId") ?? "";
var pass = builder.Configuration.GetSection("Aws").GetValue<string>("SecretAccessKey") ?? "";

builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(user, pass, Amazon.RegionEndpoint.USEast2));

builder.Services.AddHostedService<SqsProducerService>();

builder.Services.AddLogging();

var app = builder.Build();
await app.RunAsync();


static void LoadDotEnv()
{
	try
	{
		var path = FindFileUpwards(".env");
		if (path is null || !File.Exists(path))
		{
			return;
		}

		foreach (var rawLine in File.ReadAllLines(path))
		{
			var line = rawLine.Trim();
			if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
			{
				continue;
			}
			int eq = line.IndexOf('=');
			if (eq <= 0)
			{
				continue;
			}
			var key = line.Substring(0, eq).Trim();
			var value = line.Substring(eq + 1).Trim();

			// Strip optional surrounding quotes
			if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
			{
				value = value.Substring(1, value.Length - 2);
			}

			if (!string.IsNullOrEmpty(key))
			{
				Environment.SetEnvironmentVariable(key, value);
			}
		}
	}
	catch
	{
		// Best-effort: ignore .env parse errors
	}

	static string? FindFileUpwards(string fileName)
	{
		var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (dir != null)
		{
			var candidate = Path.Combine(dir.FullName, fileName);
			if (File.Exists(candidate))
			{
				return candidate;
			}
			dir = dir.Parent;
		}
		return null;
	}
}

