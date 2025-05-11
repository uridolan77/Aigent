using System.Collections.Generic;

namespace Aigent.Configuration.Core
{
    /// <summary>
    /// Implementation of IConfigurationSection
    /// </summary>
    public class ConfigurationSection : IConfigurationSection
    {
        private readonly Dictionary<string, object> _section;

        /// <summary>
        /// Initializes a new instance of the ConfigurationSection class
        /// </summary>
        /// <param name="section">Section data</param>
        public ConfigurationSection(Dictionary<string, object> section)
        {
            _section = section ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <returns>The configuration value</returns>
        public T Get<T>()
        {
            if (_section.TryGetValue("Value", out var value))
            {
                return (T)value;
            }

            return default;
        }

        /// <summary>
        /// Gets child configuration sections
        /// </summary>
        /// <returns>Child configuration sections</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            foreach (var key in _section.Keys)
            {
                if (_section[key] is Dictionary<string, object> childDict)
                {
                    yield return new ConfigurationSection(childDict)
                    {
                        Key = key
                    };
                }
            }
        }

        /// <summary>
        /// Key of the section
        /// </summary>
        public string Key { get; set; }
    }
}
