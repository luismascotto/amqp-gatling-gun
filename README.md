# AMQP Gatling Gun - Queue Processor Worker

A .NET 9 Worker Service template set up to run as a background service, listen to a queue (provider TBD), and process messages. Ships with a pluggable queue abstraction and a default "null" provider that emits synthetic messages for local testing.

## Prerequisites
- .NET SDK 9.x installed
- Docker (optional, for container builds)

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
  "Queue": {
    "Provider": "null",
    "EmitTestMessages": true,
    "EmulatedMessageIntervalSeconds": 5,
    "MaxConcurrentHandlers": 1,
    "QueueName": "tbd",
    "ConnectionString": "tbd"
  }
}
```
Replace `Provider`, `ConnectionString`, and `QueueName` when wiring a real queue provider (e.g., RabbitMQ, Azure Service Bus, etc.).

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

## Extending: Implement a real queue client
Create a new class implementing `IMessageQueueClient` in `Infrastructure/` and register it in `Program.cs` based on `QueueOptions.Provider`. Example skeleton:
```csharp
// in Infrastructure/RabbitMqQueueClient.cs
public sealed class RabbitMqQueueClient : IMessageQueueClient { /* ... */ }
```
Then, change DI registration in `Program.cs` accordingly.

## Notes
- This template favors clarity and pluggability over vendor lock-in.
- Health endpoints are not exposed by default; add ASP.NET hosting if needed for probes.
# amqp-gatling-gun
Plug in to an AMQP queue and process like a Gatling Gun avoiding bootlenecks
