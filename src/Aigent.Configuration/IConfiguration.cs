using Aigent.Core;

namespace Aigent.Configuration
{
    /// <summary>
    /// Interface for configuration
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
        string GetValue(string key);
    }
}
