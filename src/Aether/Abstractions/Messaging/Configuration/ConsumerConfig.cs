namespace Aether.Abstractions.Messaging.Configuration;

public enum DurableType
{
    Transient,
    //Ephemeral,
    Durable,
}

public record ConsumerConfig
{
    public static ConsumerConfig Default => new();

    /// <summary>
    /// Unique name for the consumer 
    /// </summary>
    public string Name { get; set; } = $"aether-{Guid.NewGuid().ToString()[..4]}";
    
    /// <summary>
    /// Indicates the type of consumer to use
    /// </summary>
    public DurableType DurableType { get; set; } = DurableType.Transient;
    
    // need to configure the NATS consumer options
    // max ack pending, etc
    
    public AckStrategy AckStrategy { get; set; } = AckStrategy.Default;
}