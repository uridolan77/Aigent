using System;
using System.Threading.Tasks;

namespace Aigent.Communication.Interfaces
{
    /// <summary>
    /// Interface for subscription handles
    /// </summary>
    public interface ISubscriptionHandle : IDisposable
    {
        /// <summary>
        /// Gets the subscription ID
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Gets the topic
        /// </summary>
        string Topic { get; }
        
        /// <summary>
        /// Gets the type of message
        /// </summary>
        Type MessageType { get; }
        
        /// <summary>
        /// Gets the creation time of the subscription
        /// </summary>
        DateTime CreatedAt { get; }
        
        /// <summary>
        /// Gets whether the subscription is active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Gets the number of messages received
        /// </summary>
        long MessageCount { get; }
        
        /// <summary>
        /// Unsubscribes from the topic
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task UnsubscribeAsync();
        
        /// <summary>
        /// Pauses the subscription
        /// </summary>
        void Pause();
        
        /// <summary>
        /// Resumes the subscription
        /// </summary>
        void Resume();
    }
}
