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

    public async Task ProcessAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        string bodyText = Encoding.UTF8.GetString(message.Body);
        _logger.LogDebug("Processing message {MessageId} with {HeaderCount} headers. Body: {Body}", message.Id, message.Headers.Count, bodyText);
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 3)), cancellationToken);
        _logger.LogDebug("Processed message {MessageId} successfully", message.Id);
    }
}


