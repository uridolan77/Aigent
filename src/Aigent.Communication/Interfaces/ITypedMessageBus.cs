using System;
using System.Threading.Tasks;

namespace Aigent.Communication.Interfaces
{
    /// <summary>
    /// Interface for typed message buses that handle a specific message type
    /// </summary>
    /// <typeparam name="T">Type of messages</typeparam>
    public interface ITypedMessageBus<T>
    {
        /// <summary>
        /// Publishes a message
        /// </summary>
        /// <param name="message">Message to publish</param>
        /// <param name="options">Optional publishing options</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task PublishAsync(T message, PublishOptions options = null);
        
        /// <summary>
        /// Subscribes to messages
        /// </summary>
        /// <param name="handler">Handler for messages</param>
        /// <param name="options">Optional subscription options</param>
        /// <returns>Subscription handle for unsubscribing</returns>
        ISubscriptionHandle SubscribeAsync(Func<T, Task> handler, SubscriptionOptions options = null);
        
        /// <summary>
        /// Gets the number of subscribers
        /// </summary>
        /// <returns>Number of subscribers</returns>
        int GetSubscriberCount();
        
        /// <summary>
        /// Configures the message bus with specific options
        /// </summary>
        /// <param name="configuration">Configuration options</param>
        void Configure(MessageBusConfiguration configuration);
    }
}
