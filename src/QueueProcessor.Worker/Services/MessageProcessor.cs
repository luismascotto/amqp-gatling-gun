using System.Text;
using QueueProcessor.Worker.Abstractions;

namespace QueueProcessor.Worker.Services;

public sealed class MessageProcessor : IMessageProcessor
{
	private readonly ILogger<MessageProcessor> _logger;

	public MessageProcessor(ILogger<MessageProcessor> logger)
	{
		_logger = logger;
	}

	public Task ProcessAsync(QueueMessage message, CancellationToken cancellationToken)
	{
		string bodyText = Encoding.UTF8.GetString(message.Body);
		_logger.LogInformation("Processed message {MessageId} with {HeaderCount} headers. Body: {Body}", message.Id, message.Headers.Count, bodyText);
		return Task.CompletedTask;
	}
}


