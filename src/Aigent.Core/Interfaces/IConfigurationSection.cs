using System.Collections.Generic;

namespace Aigent.Core.Interfaces
{
    /// <summary>
    /// Interface for configuration sections
    /// </summary>
    public interface IConfigurationSection
    {
        /// <summary>
        /// Gets the key of the section
        /// </summary>
        string Key { get; }
        
        /// <summary>
        /// Gets the path of the section
        /// </summary>
        string Path { get; }
        
        /// <summary>
        /// Gets a value from the configuration section
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>The configuration value</returns>
        T Get<T>(T defaultValue = default);
        
        /// <summary>
        /// Sets a value in the configuration section
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">Value to set</param>
        void Set<T>(T value);
        
        /// <summary>
        /// Gets a child configuration section
        /// </summary>
        /// <param name="key">Key of the child section</param>
        /// <returns>The child configuration section</returns>
        IConfigurationSection GetSection(string key);
        
        /// <summary>
        /// Gets all child configuration sections
        /// </summary>
        /// <returns>Child configuration sections</returns>
        IEnumerable<IConfigurationSection> GetChildren();
    }
}
