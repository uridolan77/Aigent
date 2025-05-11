using System.Collections.Generic;

namespace Aigent.Configuration.Core
{
    /// <summary>
    /// Interface for configuration sections, represents a portion of the configuration
    /// </summary>
    public interface IConfigurationSection
    {
        /// <summary>
        /// Key of the section
        /// </summary>
        string Key { get; }
        
        /// <summary>
        /// Gets the section value
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <returns>The configuration value</returns>
        T Get<T>();
        
        /// <summary>
        /// Gets child configuration sections
        /// </summary>
        /// <returns>Child configuration sections</returns>
        IEnumerable<IConfigurationSection> GetChildren();
    }
}
