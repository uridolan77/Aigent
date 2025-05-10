using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Monitoring;

namespace Aigent.Communication
{
    /// <summary>
    /// In-memory implementation of IMessageBus
    /// </summary>
    public class InMemoryMessageBus : IMessageBus
    {
        private readonly ConcurrentDictionary<string, List<Action<object>>> _subscriptions = new();
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;

        /// <summary>
        /// Initializes a new instance of the InMemoryMessageBus class
        /// </summary>
        /// <param name="logger">Logger for recording message bus activities</param>
        /// <param name="metrics">Metrics collector for monitoring message bus performance</param>
        public InMemoryMessageBus(ILogger logger = null, IMetricsCollector metrics = null)
        {
            _logger = logger;
            _metrics = metrics;
        }

        /// <summary>
        /// Subscribes to a topic with a message handler
        /// </summary>
        /// <param name="topic">Topic to subscribe to</param>
        /// <param name="handler">Handler to process messages</param>
        public void Subscribe(string topic, Action<object> handler)
        {
            _metrics?.StartOperation("message_bus_subscribe");
            
            try
            {
                _subscriptions.AddOrUpdate(
                    topic,
                    new List<Action<object>> { handler },
                    (_, handlers) => 
                    {
                        lock (handlers)
                        {
                            handlers.Add(handler);
                            return handlers;
                        }
                    });
                
                _logger?.Log(LogLevel.Debug, $"Subscribed to topic: {topic}");
                _metrics?.RecordMetric("message_bus.subscription_count", 1.0);
            }
            finally
            {
                _metrics?.EndOperation("message_bus_subscribe");
            }
        }

        /// <summary>
        /// Unsubscribes from a topic
        /// </summary>
        /// <param name="topic">Topic to unsubscribe from</param>
        /// <param name="handler">Handler to remove</param>
        public void Unsubscribe(string topic, Action<object> handler)
        {
            _metrics?.StartOperation("message_bus_unsubscribe");
            
            try
            {
                if (_subscriptions.TryGetValue(topic, out var handlers))
                {
                    lock (handlers)
                    {
                        handlers.Remove(handler);
                        if (!handlers.Any())
                        {
                            _subscriptions.TryRemove(topic, out _);
                        }
                    }
                }
                
                _logger?.Log(LogLevel.Debug, $"Unsubscribed from topic: {topic}");
                _metrics?.RecordMetric("message_bus.unsubscription_count", 1.0);
            }
            finally
            {
                _metrics?.EndOperation("message_bus_unsubscribe");
            }
        }

        /// <summary>
        /// Publishes a message to a topic
        /// </summary>
        /// <param name="topic">Topic to publish to</param>
        /// <param name="message">Message to publish</param>
        public async Task PublishAsync(string topic, object message)
        {
            _metrics?.StartOperation("message_bus_publish");
            
            try
            {
                if (_subscriptions.TryGetValue(topic, out var handlers))
                {
                    List<Action<object>> handlersCopy;
                    lock (handlers)
                    {
                        handlersCopy = handlers.ToList();
                    }
                    
                    var tasks = handlersCopy.Select(handler => 
                        Task.Run(() => 
                        {
                            try
                            {
                                handler(message);
                            }
                            catch (Exception ex)
                            {
                                _logger?.Log(LogLevel.Error, $"Error in message handler for topic '{topic}': {ex.Message}");
                                _metrics?.RecordMetric("message_bus.handler_error_count", 1.0);
                            }
                        }));
                    
                    await Task.WhenAll(tasks);
                    _logger?.Log(LogLevel.Debug, $"Published message to topic: {topic}");
                    _metrics?.RecordMetric("message_bus.publish_count", 1.0);
                    _metrics?.RecordMetric($"message_bus.topic.{topic}.publish_count", 1.0);
                }
                else
                {
                    _logger?.Log(LogLevel.Debug, $"No subscribers for topic: {topic}");
                    _metrics?.RecordMetric("message_bus.no_subscribers_count", 1.0);
                }
            }
            finally
            {
                _metrics?.EndOperation("message_bus_publish");
            }
        }
    }
}
