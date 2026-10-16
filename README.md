# AMQP Gatling Gun

A high-throughput queue message processor built with .NET 10. Consumes messages from AWS SQS with configurable concurrency, partitioning, and buffering strategies to avoid bottlenecks. Includes a pluggable queue abstraction that supports both live AWS SQS and a synthetic "null" provider for local testing.

## Projects

- **QueueProcessor.Worker** — Main worker service that consumes and processes messages from AWS SQS
- **QueueProcessor.ProducerTest** — Test harness to produce messages to SQS for load testing
- **ConsoleWebAppDemo** — Console-based Razor UI demo (unrelated to queue processing)

## Prerequisites

- .NET SDK 10.x installed
- Docker (optional, for container builds)
- AWS credentials (for SQS; not required with the null provider)

## Getting Started

### Run locally
```bash
# From repository root
dotnet build -c Release
dotnet run --project src/QueueProcessor.Worker
```

By default, the app uses the `NullQueueClient` and emits one synthetic message every 5 seconds. Configure this via `appsettings.json`.

### Configuration

`src/QueueProcessor.Worker/appsettings.json`:
```json
{
  "Aws": {
    "UseDefaultCredentials": true,
    "Region": "us-east-1"
  },
  "Queue": {
    "QueueName": "your-sqs-queue-name",
    "ConnectionString": "https://sqs.us-east-1.amazonaws.com/.../...",
    "MaxConcurrentHandlers": 4
  },
  "Buffering": {
    "MaxBufferedMessages": 6,
    "MaxBufferedMessagesPerTenant": 2,
    "MaxConcurrentHandlers": 2
  }
}
```

For AWS credentials, either:
- Set `UseDefaultCredentials: true` and configure AWS CLI (`aws configure`)
- Or provide `AccessKeyId` and `SecretAccessKey` in the `Aws` section (not recommended for production)

### Docker
Build from repo root:
```bash
docker build -f src/QueueProcessor.Worker/Dockerfile -t queue-processor:latest .
docker run --rm -e DOTNET_ReadyToRun=0 queue-processor:latest
```

## Project Structure
- `src/QueueProcessor.Worker`
  - `Abstractions` — Interfaces and message contracts
  - `Configuration` — Options bound from configuration
  - `Infrastructure` — Queue client implementations (currently a Null client)
  - `Services` — Message processing logic

## Extending with Other Queue Providers

The codebase currently uses AWS SQS via `AmazonSqsClient`. To add support for other queue systems (RabbitMQ, Azure Service Bus, etc.):

1. Create a new class implementing `IMessageQueueClient` in `Infrastructure/`
2. Update the DI registration in `Program.cs` to switch between implementations

Example skeleton:
```csharp
// in Infrastructure/RabbitMqQueueClient.cs
public sealed class RabbitMqQueueClient : IMessageQueueClient { /* ... */ }
```

## Running the Producer Test Harness

To generate test messages for the worker:

```bash
cd src/QueueProcessor.ProducerTest
dotnet run
```

Configure AWS credentials and queue name in `appsettings.json` before running.

## Notes

- This project favors clarity and pluggability over vendor lock-in
- Health endpoints are not exposed by default; add ASP.NET hosting if needed for probes
- The partitioned dispatcher supports multi-tenant message buffering with per-tenant concurrency limits
