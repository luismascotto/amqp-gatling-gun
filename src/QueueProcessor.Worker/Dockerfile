# syntax=docker/dockerfile:1.7-labs

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY AmqpGatlingGun.sln ./
COPY src/QueueProcessor.Worker/QueueProcessor.Worker.csproj src/QueueProcessor.Worker/
RUN dotnet restore src/QueueProcessor.Worker/QueueProcessor.Worker.csproj
COPY . .
WORKDIR /src/src/QueueProcessor.Worker
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "QueueProcessor.Worker.dll"]


