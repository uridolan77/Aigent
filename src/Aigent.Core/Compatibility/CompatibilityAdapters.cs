using System;
using Aigent.Core.Interfaces;
using Aigent.Core.Models;

namespace Aigent.Core.Compatibility
{
    /// <summary>
    /// Compatibility adapters for Core interfaces and classes
    /// </summary>
    public static class CompatibilityAdapters
    {
        /// <summary>
        /// Adapts a legacy agent builder to the new interface
        /// </summary>
        /// <param name="legacyBuilder">Legacy agent builder</param>
        /// <returns>New agent builder interface</returns>
        public static Interfaces.IAgentBuilder AdaptBuilder(IAgentBuilder legacyBuilder)
        {
            return new AgentBuilderAdapter(legacyBuilder);
        }
        
        /// <summary>
        /// Adapts a new agent builder to the legacy interface
        /// </summary>
        /// <param name="builder">New agent builder</param>
        /// <returns>Legacy agent builder interface</returns>
        public static IAgentBuilder AdaptBuilder(Interfaces.IAgentBuilder builder)
        {
            return new LegacyAgentBuilderAdapter(builder);
        }
        
        /// <summary>
        /// Adapter from legacy agent builder to new interface
        /// </summary>
        private class AgentBuilderAdapter : Interfaces.IAgentBuilder
        {
            private readonly IAgentBuilder _legacyBuilder;
            
            public AgentBuilderAdapter(IAgentBuilder legacyBuilder)
            {
                _legacyBuilder = legacyBuilder ?? throw new ArgumentNullException(nameof(legacyBuilder));
            }
            
            public Interfaces.IAgentBuilder WithConfiguration(Models.AgentConfiguration configuration)
            {
                var legacyConfig = LegacySupport.ToLegacyConfiguration(configuration);
                _legacyBuilder.WithConfiguration(legacyConfig);
                return this;
            }
            
            public Interfaces.IAgentBuilder WithMemory<T>() where T : IMemoryService
            {
                _legacyBuilder.WithMemory<T>();
                return this;
            }
            
            public Interfaces.IAgentBuilder WithMemory(IMemoryService memoryService)
            {
                // Legacy builders don't support this method directly
                // This is a best-effort implementation
                return this;
            }
            
            public Interfaces.IAgentBuilder WithGuardrail(IGuardrail guardrail)
            {
                // Legacy builders don't support this method
                return this;
            }
            
            public Interfaces.IAgentBuilder WithMLModel<T>() where T : IMLModel
            {
                // Legacy builders don't support this method
                return this;
            }
            
            public Interfaces.IAgentBuilder WithMLModel(IMLModel mlModel)
            {
                // Legacy builders don't support this method
                return this;
            }
            
            public Interfaces.IAgentBuilder WithRulesFromFile(string filePath)
            {
                // Legacy builders don't support this method
                return this;
            }
            
            public Interfaces.IAgentBuilder WithRule(string ruleName, Func<EnvironmentState, IAction> ruleAction)
            {
                // Legacy builders don't support this method
                return this;
            }
            
            public Interfaces.IAgentBuilder WithSafetyValidator(ISafetyValidator safetyValidator)
            {
                // Legacy builders don't support this method
                return this;
            }
            
            public Interfaces.IAgentBuilder WithOption(string key, object value)
            {
                // Legacy builders don't support this method
                return this;
            }
            
            public IAgent Build()
            {
                var legacyAgent = _legacyBuilder.Build();
                return new AgentAdapter(legacyAgent);
            }
        }
        
        /// <summary>
        /// Adapter from new agent builder to legacy interface
        /// </summary>
        private class LegacyAgentBuilderAdapter : IAgentBuilder
        {
            private readonly Interfaces.IAgentBuilder _builder;
            
            public LegacyAgentBuilderAdapter(Interfaces.IAgentBuilder builder)
            {
                _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            }
            
            public IAgentBuilder WithConfiguration(AgentConfiguration configuration)
            {
                var newConfig = LegacySupport.FromLegacyConfiguration(configuration);
                _builder.WithConfiguration(newConfig);
                return this;
            }
            
            public IAgentBuilder WithMemory<T>() where T : IMemoryService
            {
                _builder.WithMemory<T>();
                return this;
            }
            
            public IAgent Build()
            {
                var agent = _builder.Build();
                return new LegacyAgentAdapter(agent);
            }
        }
        
        /// <summary>
        /// Adapter from legacy agent to new interface
        /// </summary>
        private class AgentAdapter : Interfaces.IAgent
        {
            private readonly IAgent _legacyAgent;
            
            public AgentAdapter(IAgent legacyAgent)
            {
                _legacyAgent = legacyAgent ?? throw new ArgumentNullException(nameof(legacyAgent));
            }
            
            public string Id => _legacyAgent.Id;
            
            public string Name => _legacyAgent.Name;
            
            public AgentType Type => _legacyAgent.Type;
            
            public AgentStatus Status => _legacyAgent.Status;
            
            public AgentCapabilities Capabilities => _legacyAgent.Capabilities;
            
            public Models.AgentConfiguration Configuration 
            { 
                get 
                {
                    // Create a default configuration from the available properties
                    return new Models.AgentConfiguration
                    {
                        Name = Name,
                        Type = Type,
                        Version = "1.0",
                        Enabled = true
                    };
                }
            }
            
            public System.Collections.Generic.Dictionary<string, object> Metadata => new();
            
            public Task Initialize() => _legacyAgent.Initialize();
            
            public Task<IAction> DecideAction(EnvironmentState state) => _legacyAgent.DecideAction(state);
            
            public Task Learn(EnvironmentState state, IAction action, ActionResult result) => _legacyAgent.Learn(state, action, result);
            
            public Task<CommandResult> ProcessCommand(AgentCommand command)
            {
                // Legacy agents don't support commands directly
                return Task.FromResult(new CommandResult
                {
                    Success = false,
                    Message = "Command not supported by legacy agent"
                });
            }
            
            public Task Shutdown() => _legacyAgent.Shutdown();
            
            public void Dispose() => _legacyAgent.Dispose();
        }
        
        /// <summary>
        /// Adapter from new agent to legacy interface
        /// </summary>
        private class LegacyAgentAdapter : IAgent
        {
            private readonly Interfaces.IAgent _agent;
            
            public LegacyAgentAdapter(Interfaces.IAgent agent)
            {
                _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            }
            
            public string Id => _agent.Id;
            
            public string Name => _agent.Name;
            
            public AgentType Type => _agent.Type;
            
            public AgentStatus Status => _agent.Status;
            
            public AgentCapabilities Capabilities => _agent.Capabilities;
            
            public Task Initialize() => _agent.Initialize();
            
            public Task<IAction> DecideAction(EnvironmentState state) => _agent.DecideAction(state);
            
            public Task Learn(EnvironmentState state, IAction action, ActionResult result) => _agent.Learn(state, action, result);
            
            public Task Shutdown() => _agent.Shutdown();
            
            public void Dispose() => _agent.Dispose();
        }
    }
}
