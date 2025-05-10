using System;
using System.Threading.Tasks;

namespace Aigent.Communication
{
    /// <summary>
    /// Interface for message buses
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Publishes a message
        /// </summary>
        /// <typeparam name="T">Type of the message</typeparam>
        /// <param name="topic">Topic to publish to</param>
        /// <param name="message">Message to publish</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task Publish<T>(string topic, T message);
        
        /// <summary>
        /// Subscribes to a topic
        /// </summary>
        /// <typeparam name="T">Type of the message</typeparam>
        /// <param name="topic">Topic to subscribe to</param>
        /// <param name="handler">Handler for messages</param>
        /// <returns>Subscription ID</returns>
        string Subscribe<T>(string topic, Func<T, Task> handler);
        
        /// <summary>
        /// Unsubscribes from a topic
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task Unsubscribe(string subscriptionId);
    }
}
