namespace QueueProcessor.Worker.Abstractions;

public sealed record QueueMessage(
	string Id,
	byte[] Body,
	IReadOnlyDictionary<string, string> Headers
);


