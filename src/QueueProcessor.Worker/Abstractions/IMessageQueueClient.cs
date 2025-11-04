namespace QueueProcessor.Worker.Abstractions;

public interface IMessageQueueClient
{
	IAsyncEnumerable<QueueMessage> ReadMessagesAsync(CancellationToken cancellationToken);
	Task<bool> AcceptMessageAsync(QueueMessage message, CancellationToken cancellationToken);
	Task<bool> SkipMessageAsync(QueueMessage message, CancellationToken cancellationToken);
}


