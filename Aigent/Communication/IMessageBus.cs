using System;
using System.Threading.Tasks;

namespace Aigent.Communication
{
    /// <summary>
    /// Interface for message bus services that enable inter-agent communication
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Subscribes to a topic with a message handler
        /// </summary>
        /// <param name="topic">Topic to subscribe to</param>
        /// <param name="handler">Handler to process messages</param>
        void Subscribe(string topic, Action<object> handler);
        
        /// <summary>
        /// Unsubscribes from a topic
        /// </summary>
        /// <param name="topic">Topic to unsubscribe from</param>
        /// <param name="handler">Handler to remove</param>
        void Unsubscribe(string topic, Action<object> handler);
        
        /// <summary>
        /// Publishes a message to a topic
        /// </summary>
        /// <param name="topic">Topic to publish to</param>
        /// <param name="message">Message to publish</param>
        Task PublishAsync(string topic, object message);
    }
}
