using System;
using System.Collections.Generic;

namespace Aigent.Communication.Models
{
    /// <summary>
    /// Configuration for message buses
    /// </summary>
    public class MessageBusConfiguration
    {
        /// <summary>
        /// Gets or sets whether to enable logging
        /// </summary>
        public bool EnableLogging { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to enable metrics
        /// </summary>
        public bool EnableMetrics { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the default delivery mode
        /// </summary>
        public DeliveryMode DefaultDeliveryMode { get; set; } = DeliveryMode.AtLeastOnce;
        
        /// <summary>
        /// Gets or sets the default message timeout in seconds
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Gets or sets the maximum message size in bytes
        /// </summary>
        public int MaxMessageSizeBytes { get; set; } = 1024 * 1024; // 1 MB
        
        /// <summary>
        /// Gets or sets the maximum number of subscribers per topic
        /// </summary>
        public int MaxSubscribersPerTopic { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets the maximum number of messages in memory queue
        /// </summary>
        public int MaxQueueSize { get; set; } = 1000;
        
        /// <summary>
        /// Gets or sets whether to enable message persistence
        /// </summary>
        public bool EnablePersistence { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the persistence path for message storage
        /// </summary>
        public string PersistencePath { get; set; } = "messages";
        
        /// <summary>
        /// Gets or sets additional options
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Delivery modes for messages
    /// </summary>
    public enum DeliveryMode
    {
        /// <summary>
        /// Best effort delivery, message may be lost
        /// </summary>
        BestEffort,
        
        /// <summary>
        /// Message is delivered at least once, may be delivered multiple times
        /// </summary>
        AtLeastOnce,
        
        /// <summary>
        /// Message is delivered exactly once
        /// </summary>
        ExactlyOnce
    }
}
