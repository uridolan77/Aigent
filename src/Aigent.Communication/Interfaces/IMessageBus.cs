using System;
using System.Threading.Tasks;

namespace Aigent.Communication.Interfaces
{
    /// <summary>
    /// Interface for message bus services
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Publishes a message to a topic
        /// </summary>
        /// <typeparam name="T">Type of the message</typeparam>
        /// <param name="topic">Topic to publish to</param>
        /// <param name="message">Message to publish</param>
        /// <param name="options">Optional publishing options</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task PublishAsync<T>(string topic, T message, PublishOptions options = null);
        
        /// <summary>
        /// Subscribes to a topic
        /// </summary>
        /// <typeparam name="T">Type of the message</typeparam>
        /// <param name="topic">Topic to subscribe to</param>
        /// <param name="handler">Handler for messages</param>
        /// <param name="options">Optional subscription options</param>
        /// <returns>Subscription handle for unsubscribing</returns>
        ISubscriptionHandle SubscribeAsync<T>(string topic, Func<T, Task> handler, SubscriptionOptions options = null);
        
        /// <summary>
        /// Gets the number of subscribers for a topic
        /// </summary>
        /// <param name="topic">Topic to check</param>
        /// <returns>Number of subscribers</returns>
        int GetSubscriberCount(string topic);
        
        /// <summary>
        /// Gets a list of active topics
        /// </summary>
        /// <returns>List of active topics</returns>
        string[] GetActiveTopics();
        
        /// <summary>
        /// Configures the message bus with specific options
        /// </summary>
        /// <param name="configuration">Configuration options</param>
        void Configure(MessageBusConfiguration configuration);
    }
}
