using System;
using System.Collections.Generic;
using System.Linq;
using Aigent.Core.Interfaces;

namespace Aigent.Core.Configuration
{
    /// <summary>
    /// Default implementation of IConfigurationSection
    /// </summary>
    public class ConfigurationSection : IConfigurationSection
    {
        private readonly Dictionary<string, object> _values = new();
        private readonly Dictionary<string, ConfigurationSection> _children = new();
        
        /// <summary>
        /// Initializes a new instance of the ConfigurationSection class
        /// </summary>
        /// <param name="key">Key for this section</param>
        /// <param name="parentPath">Path of the parent section</param>
        public ConfigurationSection(string key, string parentPath = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Path = string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}:{key}";
        }
        
        /// <summary>
        /// Gets the key of the section
        /// </summary>
        public string Key { get; }
        
        /// <summary>
        /// Gets the path of the section
        /// </summary>
        public string Path { get; }
        
        /// <summary>
        /// Gets a value from the configuration section
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>The configuration value</returns>
        public T Get<T>(T defaultValue = default)
        {
            if (_values.TryGetValue(Key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                
                // Try conversion
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Sets a value in the configuration section
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">Value to set</param>
        public void Set<T>(T value)
        {
            _values[Key] = value;
        }
        
        /// <summary>
        /// Gets a child configuration section
        /// </summary>
        /// <param name="key">Key of the child section</param>
        /// <returns>The child configuration section</returns>
        public IConfigurationSection GetSection(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }
            
            if (!_children.TryGetValue(key, out var section))
            {
                section = new ConfigurationSection(key, Path);
                _children[key] = section;
            }
            
            return section;
        }
        
        /// <summary>
        /// Gets all child configuration sections
        /// </summary>
        /// <returns>Child configuration sections</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _children.Values.Cast<IConfigurationSection>();
        }
    }
}
