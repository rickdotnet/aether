using Aether.Abstractions.Messaging;

namespace ConsoleDemo.Endpoints;

public record SomethingHappenedCommand(string Message) : ICommand;
