using Aether.Abstractions.Messaging;

namespace Scenarios.Endpoints;

public record SomethingHappenedCommand(string Message) : ICommand;
