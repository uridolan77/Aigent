using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Monitoring;

namespace Aigent.Communication
{
    /// <summary>
    /// In-memory message bus implementation
    /// </summary>
    public class InMemoryMessageBus : IMessageBus
    {
        private readonly Dictionary<string, List<Subscription>> _subscriptions = new();
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the InMemoryMessageBus class
        /// </summary>
        /// <param name="logger">Logger</param>
        public InMemoryMessageBus(ILogger logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Publishes a message
        /// </summary>
        /// <typeparam name="T">Type of the message</typeparam>
        /// <param name="topic">Topic to publish to</param>
        /// <param name="message">Message to publish</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task Publish<T>(string topic, T message)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
            }
            
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            
            if (!_subscriptions.TryGetValue(topic, out var topicSubscriptions))
            {
                _logger?.Log(LogLevel.Debug, $"No subscribers for topic: {topic}");
                return;
            }
            
            var tasks = new List<Task>();
            
            foreach (var subscription in topicSubscriptions)
            {
                if (subscription.MessageType == typeof(T))
                {
                    try
                    {
                        var handler = (Func<T, Task>)subscription.Handler;
                        tasks.Add(handler(message));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Error handling message for topic {topic}: {ex.Message}", ex);
                    }
                }
            }
            
            await Task.WhenAll(tasks);
            
            _logger?.Log(LogLevel.Debug, $"Published message to {topicSubscriptions.Count} subscribers for topic: {topic}");
        }
        
        /// <summary>
        /// Subscribes to a topic
        /// </summary>
        /// <typeparam name="T">Type of the message</typeparam>
        /// <param name="topic">Topic to subscribe to</param>
        /// <param name="handler">Handler for messages</param>
        /// <returns>Subscription ID</returns>
        public string Subscribe<T>(string topic, Func<T, Task> handler)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
            }
            
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            
            var subscriptionId = Guid.NewGuid().ToString();
            
            var subscription = new Subscription
            {
                Id = subscriptionId,
                Topic = topic,
                MessageType = typeof(T),
                Handler = handler
            };
            
            lock (_subscriptions)
            {
                if (!_subscriptions.TryGetValue(topic, out var topicSubscriptions))
                {
                    topicSubscriptions = new List<Subscription>();
                    _subscriptions[topic] = topicSubscriptions;
                }
                
                topicSubscriptions.Add(subscription);
            }
            
            _logger?.Log(LogLevel.Debug, $"Subscribed to topic: {topic} with ID: {subscriptionId}");
            
            return subscriptionId;
        }
        
        /// <summary>
        /// Unsubscribes from a topic
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task Unsubscribe(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentException("Subscription ID cannot be null or empty", nameof(subscriptionId));
            }
            
            lock (_subscriptions)
            {
                foreach (var topic in _subscriptions.Keys)
                {
                    var topicSubscriptions = _subscriptions[topic];
                    var subscription = topicSubscriptions.Find(s => s.Id == subscriptionId);
                    
                    if (subscription != null)
                    {
                        topicSubscriptions.Remove(subscription);
                        _logger?.Log(LogLevel.Debug, $"Unsubscribed from topic: {topic} with ID: {subscriptionId}");
                        break;
                    }
                }
            }
            
            return Task.CompletedTask;
        }
        
        private class Subscription
        {
            public string Id { get; set; }
            public string Topic { get; set; }
            public Type MessageType { get; set; }
            public object Handler { get; set; }
        }
    }
}
