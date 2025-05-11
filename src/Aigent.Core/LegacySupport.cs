using Aigent.Core.Configuration;
using Aigent.Core.Interfaces;
using Aigent.Core.Models;

namespace Aigent.Core
{
    /// <summary>
    /// Legacy support to maintain backward compatibility with older code
    /// </summary>
    public static class LegacySupport
    {
        /// <summary>
        /// Converts a new style configuration to the legacy format
        /// </summary>
        /// <param name="configuration">New style configuration</param>
        /// <returns>Legacy format configuration</returns>
        public static AgentConfiguration ToLegacyConfiguration(Models.AgentConfiguration configuration)
        {
            var legacy = new AgentConfiguration
            {
                Name = configuration.Name,
                Type = configuration.Type,
                Rules = configuration.Rules,
                Settings = configuration.Settings
            };
            
            return legacy;
        }
        
        /// <summary>
        /// Converts a legacy configuration to the new style format
        /// </summary>
        /// <param name="legacy">Legacy format configuration</param>
        /// <returns>New style configuration</returns>
        public static Models.AgentConfiguration FromLegacyConfiguration(AgentConfiguration legacy)
        {
            var configuration = new Models.AgentConfiguration
            {
                Name = legacy.Name,
                Type = legacy.Type,
                Rules = legacy.Rules,
                Settings = legacy.Settings,
                // New properties get default values
                Version = "1.0",
                Enabled = true
            };
            
            return configuration;
        }
        
        /// <summary>
        /// Converts a new style configuration section to the legacy format
        /// </summary>
        /// <param name="section">New style configuration section</param>
        /// <returns>Legacy format configuration section</returns>
        public static IConfigurationSection ToLegacySection(Interfaces.IConfigurationSection section)
        {
            // This adapter wraps the new IConfigurationSection to expose the legacy interface
            return new LegacyConfigurationSectionAdapter(section);
        }
        
        /// <summary>
        /// Adapter that makes new IConfigurationSection look like the legacy one
        /// </summary>
        private class LegacyConfigurationSectionAdapter : IConfigurationSection
        {
            private readonly Interfaces.IConfigurationSection _section;
            
            public LegacyConfigurationSectionAdapter(Interfaces.IConfigurationSection section)
            {
                _section = section;
            }
            
            public string Key { get => _section.Key; set { } }
            
            public T Get<T>()
            {
                return _section.Get<T>();
            }
            
            public System.Collections.Generic.IEnumerable<IConfigurationSection> GetChildren()
            {
                // Recursively adapt all children
                foreach (var child in _section.GetChildren())
                {
                    yield return new LegacyConfigurationSectionAdapter(child);
                }
            }
        }
    }
}
