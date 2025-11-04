using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Configuration;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace QueueProcessor.Worker.Infrastructure;

public sealed class NullQueueClient : IMessageQueueClient
{
	private readonly ILogger<NullQueueClient> _logger;
	private readonly QueueOptions _options;

	public NullQueueClient(ILogger<NullQueueClient> logger, IOptions<QueueOptions> options)
	{
		_logger = logger;
		_options = options.Value;
	}

    public Task<bool> AcceptMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[NullQueueClient] Accepting message {MessageId}", message.Id);
        return Task.FromResult(true);
    }

    public async IAsyncEnumerable<QueueMessage> ReadMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (!_options.EmitTestMessages)
		{
			_logger.LogInformation("NullQueueClient is active. EmitTestMessages=false, no messages will be produced.");
			while (!cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
			}
			yield break;
		}

		_logger.LogWarning("NullQueueClient emitting synthetic messages every {Seconds}s. Replace with a real provider when ready.", _options.EmulatedMessageIntervalSeconds);
		int counter = 0;
		while (!cancellationToken.IsCancellationRequested)
		{
			counter++;
			var messageId = Guid.NewGuid().ToString("n");
			var body = System.Text.Encoding.UTF8.GetBytes($"Synthetic message #{counter} at {DateTimeOffset.UtcNow:O}");
			var headers = new Dictionary<string, string>
			{
				["x-provider"] = _options.Provider ?? "null",
				["x-counter"] = counter.ToString()
			};
			yield return new QueueMessage(messageId, body, headers);
			await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.EmulatedMessageIntervalSeconds)), cancellationToken);
		}
	}

    public Task<bool> SkipMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[NullQueueClient] Skipping message {MessageId}", message.Id);
        return Task.FromResult(true);
    }
}


