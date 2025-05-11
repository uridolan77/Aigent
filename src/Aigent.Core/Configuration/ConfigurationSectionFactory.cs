using System;
using Aigent.Core.Interfaces;

namespace Aigent.Core.Configuration
{
    /// <summary>
    /// Factory for creating configuration sections
    /// </summary>
    public class ConfigurationSectionFactory : IConfigurationSectionFactory
    {
        /// <summary>
        /// Creates a configuration section
        /// </summary>
        /// <param name="key">Key for the section</param>
        /// <param name="parentPath">Optional parent path</param>
        /// <returns>A new configuration section</returns>
        public IConfigurationSection CreateSection(string key, string parentPath = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }
            
            return new ConfigurationSection(key, parentPath);
        }
    }
}
