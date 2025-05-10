using System;
using System.Threading.Tasks;
using Aigent.Memory;
using Aigent.Safety;
using Aigent.Communication;
using Aigent.Monitoring;

namespace Aigent.Core
{
    /// <summary>
    /// Base implementation of the IAgent interface providing common functionality
    /// </summary>
    public abstract class BaseAgent : IAgent
    {
        /// <summary>
        /// Unique identifier for the agent
        /// </summary>
        public string Id { get; protected set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Human-readable name of the agent
        /// </summary>
        public string Name { get; protected set; }
        
        /// <summary>
        /// Type of agent based on its decision-making approach
        /// </summary>
        public abstract AgentType Type { get; }
        
        /// <summary>
        /// Status of the agent
        /// </summary>
        public AgentStatus Status { get; protected set; } = AgentStatus.Initializing;
        
        /// <summary>
        /// Agent capabilities including supported actions and skill levels
        /// </summary>
        public AgentCapabilities Capabilities { get; protected set; } = new AgentCapabilities();

        /// <summary>
        /// Memory service for storing and retrieving context
        /// </summary>
        protected readonly IMemoryService _memory;
        
        /// <summary>
        /// Safety validator for ensuring actions are safe
        /// </summary>
        protected readonly ISafetyValidator _safetyValidator;
        
        /// <summary>
        /// Logger for recording agent activities
        /// </summary>
        protected readonly ILogger _logger;
        
        /// <summary>
        /// Message bus for inter-agent communication
        /// </summary>
        protected readonly IMessageBus _messageBus;
        
        /// <summary>
        /// Metrics collector for monitoring agent performance
        /// </summary>
        protected readonly IMetricsCollector _metrics;
        
        /// <summary>
        /// Flag indicating whether the agent has been disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the BaseAgent class
        /// </summary>
        /// <param name="memory">Memory service for storing and retrieving context</param>
        /// <param name="safetyValidator">Safety validator for ensuring actions are safe</param>
        /// <param name="logger">Logger for recording agent activities</param>
        /// <param name="messageBus">Message bus for inter-agent communication</param>
        /// <param name="metrics">Metrics collector for monitoring agent performance</param>
        protected BaseAgent(
            IMemoryService memory, 
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus,
            IMetricsCollector metrics = null)
        {
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _safetyValidator = safetyValidator ?? throw new ArgumentNullException(nameof(safetyValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _metrics = metrics;
        }

        /// <summary>
        /// Initializes the agent and its resources
        /// </summary>
        public virtual async Task Initialize()
        {
            _metrics?.StartOperation($"agent_{Id}_initialize");
            
            try
            {
                await _memory.Initialize(Id);
                _messageBus.Subscribe($"agent.{Id}.command", HandleMessage);
                _logger.Log(LogLevel.Information, $"Agent {Name} initialized");
                
                Status = AgentStatus.Ready;
                _metrics?.RecordMetric($"agent.{Id}.initialization", 1.0);
            }
            catch (Exception ex)
            {
                Status = AgentStatus.Error;
                _logger.LogError($"Error initializing agent {Name}: {ex.Message}", ex);
                _metrics?.RecordMetric($"agent.{Id}.initialization_error", 1.0);
                throw;
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_initialize");
            }
        }

        /// <summary>
        /// Decides on an action based on the current environment state
        /// </summary>
        /// <param name="state">Current state of the environment</param>
        /// <returns>The action to be performed</returns>
        public abstract Task<IAction> DecideAction(EnvironmentState state);
        
        /// <summary>
        /// Learns from the result of an action
        /// </summary>
        /// <param name="state">State when the action was performed</param>
        /// <param name="action">The action that was performed</param>
        /// <param name="result">The result of the action</param>
        public virtual async Task Learn(EnvironmentState state, IAction action, ActionResult result)
        {
            // Base implementation does not learn
            // Derived classes should override this method to implement learning
            await Task.CompletedTask;
        }

        /// <summary>
        /// Shuts down the agent and releases resources
        /// </summary>
        public virtual async Task Shutdown()
        {
            _metrics?.StartOperation($"agent_{Id}_shutdown");
            
            try
            {
                Status = AgentStatus.ShuttingDown;
                _logger.Log(LogLevel.Information, $"Shutting down agent {Name}");
                await _memory.Flush();
                _messageBus.Unsubscribe($"agent.{Id}.command", HandleMessage);
                
                _metrics?.RecordMetric($"agent.{Id}.shutdown", 1.0);
            }
            catch (Exception ex)
            {
                Status = AgentStatus.Error;
                _logger.LogError($"Error shutting down agent {Name}: {ex.Message}", ex);
                _metrics?.RecordMetric($"agent.{Id}.shutdown_error", 1.0);
                throw;
            }
            finally
            {
                _metrics?.EndOperation($"agent_{Id}_shutdown");
            }
        }

        /// <summary>
        /// Handles messages received from the message bus
        /// </summary>
        /// <param name="message">The message received</param>
        protected virtual void HandleMessage(object message)
        {
            _logger.Log(LogLevel.Debug, $"Agent {Name} received message: {message}");
        }

        /// <summary>
        /// Disposes the agent and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the agent and releases resources
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Shutdown().GetAwaiter().GetResult();
                }
                _disposed = true;
            }
        }
    }
}
