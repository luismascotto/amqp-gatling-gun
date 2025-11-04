namespace QueueProcessor.Worker.Abstractions;

public interface IMessageQueueClient
{
	IAsyncEnumerable<QueueMessage> ReadMessagesAsync(CancellationToken cancellationToken);
}


