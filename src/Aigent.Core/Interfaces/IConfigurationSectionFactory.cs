namespace Aigent.Core.Interfaces
{
    /// <summary>
    /// Interface for configuration section factories
    /// </summary>
    public interface IConfigurationSectionFactory
    {
        /// <summary>
        /// Creates a configuration section
        /// </summary>
        /// <param name="key">Key for the section</param>
        /// <param name="parentPath">Optional parent path</param>
        /// <returns>A new configuration section</returns>
        IConfigurationSection CreateSection(string key, string parentPath = null);
    }
}
