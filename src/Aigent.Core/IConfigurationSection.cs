using System.Collections.Generic;

namespace Aigent.Core
{
    /// <summary>
    /// Interface for configuration sections
    /// </summary>
    public interface IConfigurationSection
    {
        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <returns>The configuration value</returns>
        T Get<T>();
        
        /// <summary>
        /// Gets child configuration sections
        /// </summary>
        /// <returns>Child configuration sections</returns>
        IEnumerable<IConfigurationSection> GetChildren();
        
        /// <summary>
        /// Key of the section
        /// </summary>
        string Key { get; set; }
    }
}
