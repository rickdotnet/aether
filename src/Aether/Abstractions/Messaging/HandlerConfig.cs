namespace Aether.Abstractions.Messaging;

public record HandlerConfig
{
    public static HandlerConfig Default { get; } = new();
    public int MaxProcessors { get; init; } = 1;
    public int MessageBufferCapacity { get; init; } = 1000;
    
    public bool Validate()
    {
        if (MaxProcessors < 1)
           return false;
        if (MessageBufferCapacity < 1)
            return false;
        
        return true;
    }
    
    public static HandlerConfig Concurrent(int maxProcessors)
        => new() { MaxProcessors = maxProcessors };
}
