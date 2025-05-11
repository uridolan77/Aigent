using System.Collections.Generic;

namespace Aigent.Configuration.Core
{
    /// <summary>
    /// Interface for configuration services, provides access to app configuration values
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Gets a configuration section by key
        /// </summary>
        /// <param name="key">Key of the section</param>
        /// <returns>The configuration section</returns>
        IConfigurationSection GetSection(string key);

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        string GetValue(string key);
        
        /// <summary>
        /// Gets a strongly typed configuration value
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        T GetValue<T>(string key);
        
        /// <summary>
        /// Gets a strongly typed configuration value
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key of the value</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>The configuration value or default</returns>
        T GetValue<T>(string key, T defaultValue);
    }
}
