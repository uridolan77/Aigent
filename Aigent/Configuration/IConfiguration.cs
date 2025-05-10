using System.Collections.Generic;

namespace Aigent.Configuration
{
    /// <summary>
    /// Interface for configuration services
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Gets a configuration section
        /// </summary>
        /// <param name="key">Key of the section</param>
        /// <returns>The configuration section</returns>
        IConfigurationSection GetSection(string key);
        
        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        string this[string key] { get; }
    }

    /// <summary>
    /// Interface for configuration sections
    /// </summary>
    public interface IConfigurationSection : IConfiguration
    {
        /// <summary>
        /// Key of the section
        /// </summary>
        string Key { get; }
        
        /// <summary>
        /// Path of the section
        /// </summary>
        string Path { get; }
        
        /// <summary>
        /// Value of the section
        /// </summary>
        string Value { get; }
        
        /// <summary>
        /// Gets the children of the section
        /// </summary>
        /// <returns>The children of the section</returns>
        IEnumerable<IConfigurationSection> GetChildren();
        
        /// <summary>
        /// Gets a typed value from the section
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <returns>The typed value</returns>
        T Get<T>();
    }
}
