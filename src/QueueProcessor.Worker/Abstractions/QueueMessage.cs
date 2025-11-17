namespace QueueProcessor.Worker.Abstractions;

public sealed record QueueMessage(
	string Id,
	string ReceiptId,
	byte[] Body,
	IReadOnlyDictionary<string, string> Headers
);


