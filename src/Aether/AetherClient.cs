using Aether.Messaging;

namespace Aether;

public class AetherClient
{
    public IDefaultMessagingProvider Messaging { get; } = null!;

}