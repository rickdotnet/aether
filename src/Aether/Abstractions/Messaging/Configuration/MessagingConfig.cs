﻿namespace Aether.Abstractions.Messaging.Configuration;

/// <summary>
/// Base configuration for Aether
/// </summary>
public record MessagingConfig
{
    /// <summary>
    /// Unique identifier for the process
    /// <remarks>Used to track message origination</remarks>
    /// </summary>
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Default consumer name to use when not specified by Endpoints
    /// </summary>
    public string? DefaultConsumerName { get; set; }
    
    /// <summary>
    /// Default namespace to use when not specified by Endpoints
    /// </summary>
    public string? DefaultNamespace { get; set; }
    
    /// <summary>
    /// Subscribers will create missing resources if set to true
    /// </summary>
    public bool CreateMissingResources { get; set; }
    
    /// <summary>
    /// Publish only mode
    /// </summary>
    public bool PublishOnly { get; set; }
}