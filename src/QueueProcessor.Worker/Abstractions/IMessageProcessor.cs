namespace QueueProcessor.Worker.Abstractions;

public interface IMessageProcessor
{
	Task ProcessAsync(QueueMessage message, CancellationToken cancellationToken);
}


