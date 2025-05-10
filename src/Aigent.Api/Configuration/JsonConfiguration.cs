using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Aigent.Configuration;

namespace Aigent.Api.Configuration
{
    /// <summary>
    /// Adapter for Microsoft.Extensions.Configuration.IConfiguration to Aigent.Configuration.IConfiguration
    /// </summary>
    public class JsonConfiguration : Aigent.Configuration.IConfiguration
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the JsonConfiguration class
        /// </summary>
        /// <param name="configuration">Microsoft configuration</param>
        public JsonConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        public string this[string key] => _configuration[key];

        /// <summary>
        /// Gets a configuration section
        /// </summary>
        /// <param name="key">Key of the section</param>
        /// <returns>The configuration section</returns>
        public Aigent.Configuration.IConfigurationSection GetSection(string key)
        {
            var section = _configuration.GetSection(key);
            return new JsonConfigurationSection(section);
        }
    }

    /// <summary>
    /// Adapter for Microsoft.Extensions.Configuration.IConfigurationSection to Aigent.Configuration.IConfigurationSection
    /// </summary>
    public class JsonConfigurationSection : Aigent.Configuration.IConfigurationSection
    {
        private readonly Microsoft.Extensions.Configuration.IConfigurationSection _section;

        /// <summary>
        /// Initializes a new instance of the JsonConfigurationSection class
        /// </summary>
        /// <param name="section">Microsoft configuration section</param>
        public JsonConfigurationSection(Microsoft.Extensions.Configuration.IConfigurationSection section)
        {
            _section = section ?? throw new ArgumentNullException(nameof(section));
        }

        /// <summary>
        /// Key of the section
        /// </summary>
        public string Key => _section.Key;

        /// <summary>
        /// Path of the section
        /// </summary>
        public string Path => _section.Path;

        /// <summary>
        /// Value of the section
        /// </summary>
        public string Value => _section.Value;

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        public string this[string key] => _section[key];

        /// <summary>
        /// Gets the children of the section
        /// </summary>
        /// <returns>The children of the section</returns>
        public IEnumerable<Aigent.Configuration.IConfigurationSection> GetChildren()
        {
            return _section.GetChildren().Select(s => new JsonConfigurationSection(s));
        }

        /// <summary>
        /// Gets a configuration section
        /// </summary>
        /// <param name="key">Key of the section</param>
        /// <returns>The configuration section</returns>
        public Aigent.Configuration.IConfigurationSection GetSection(string key)
        {
            return new JsonConfigurationSection(_section.GetSection(key));
        }

        /// <summary>
        /// Gets a typed value from the section
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <returns>The typed value</returns>
        public T Get<T>()
        {
            return _section.Get<T>();
        }
    }
}
