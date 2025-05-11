using System;
using System.Collections.Generic;

namespace Aigent.Communication.Models
{
    /// <summary>
    /// Options for publishing messages
    /// </summary>
    public class PublishOptions
    {
        /// <summary>
        /// Gets or sets the message ID
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the correlation ID for tracking related messages
        /// </summary>
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets the message priority
        /// </summary>
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;
        
        /// <summary>
        /// Gets or sets the time-to-live in seconds
        /// </summary>
        public int TimeToLiveSeconds { get; set; } = 60;
        
        /// <summary>
        /// Gets or sets the delivery mode
        /// </summary>
        public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.AtLeastOnce;
        
        /// <summary>
        /// Gets or sets when the message should be delivered
        /// </summary>
        public DateTime? DeliverAt { get; set; }
        
        /// <summary>
        /// Gets or sets additional headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Creates publish options with default settings
        /// </summary>
        /// <returns>Publish options with default settings</returns>
        public static PublishOptions Default()
        {
            return new PublishOptions();
        }
        
        /// <summary>
        /// Creates publish options with high priority
        /// </summary>
        /// <returns>Publish options with high priority</returns>
        public static PublishOptions HighPriority()
        {
            return new PublishOptions { Priority = MessagePriority.High };
        }
        
        /// <summary>
        /// Creates publish options with persistence enabled
        /// </summary>
        /// <returns>Publish options with persistence enabled</returns>
        public static PublishOptions Persistent()
        {
            return new PublishOptions { DeliveryMode = DeliveryMode.ExactlyOnce };
        }
    }
    
    /// <summary>
    /// Priority levels for messages
    /// </summary>
    public enum MessagePriority
    {
        /// <summary>
        /// Low priority
        /// </summary>
        Low = 0,
        
        /// <summary>
        /// Normal priority
        /// </summary>
        Normal = 1,
        
        /// <summary>
        /// High priority
        /// </summary>
        High = 2,
        
        /// <summary>
        /// Critical priority
        /// </summary>
        Critical = 3
    }
}
