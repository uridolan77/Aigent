using System;

namespace Aigent.Communication.Interfaces
{
    /// <summary>
    /// Interface for factories that create message buses
    /// </summary>
    public interface IMessageBusFactory
    {
        /// <summary>
        /// Creates a message bus
        /// </summary>
        /// <returns>A new message bus</returns>
        IMessageBus CreateMessageBus();
        
        /// <summary>
        /// Creates a message bus with a specific configuration
        /// </summary>
        /// <param name="configuration">The configuration for the message bus</param>
        /// <returns>A new message bus</returns>
        IMessageBus CreateMessageBus(MessageBusConfiguration configuration);
        
        /// <summary>
        /// Creates a typed message bus for a specific message type
        /// </summary>
        /// <typeparam name="T">Type of messages</typeparam>
        /// <returns>A typed message bus</returns>
        ITypedMessageBus<T> CreateTypedMessageBus<T>();
    }
}
