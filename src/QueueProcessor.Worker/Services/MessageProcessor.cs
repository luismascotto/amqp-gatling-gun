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
        //string bodyText = Encoding.UTF8.GetString(message.Body);
        await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(120, 240)), cancellationToken);
        _logger.LogDebug("Processed message {MessageId} successfully", message.Id);
    }
}


