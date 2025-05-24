namespace Aether.Abstractions.Messaging;

// ReSharper disable once UnusedTypeParameter
public interface IRequest : IMessage;
public interface IRequest<T> :  IRequest { }