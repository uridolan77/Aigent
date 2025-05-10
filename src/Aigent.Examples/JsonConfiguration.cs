using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Aigent.Configuration;
using Aigent.Core;

namespace Aigent.Examples
{
    /// <summary>
    /// Simple JSON-based configuration implementation for examples
    /// </summary>
    public class JsonConfiguration : IConfiguration
    {
        private readonly Dictionary<string, object> _configuration;

        /// <summary>
        /// Initializes a new instance of the JsonConfiguration class
        /// </summary>
        /// <param name="filePath">Path to the JSON configuration file</param>
        public JsonConfiguration(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file not found: {filePath}");
            }

            var json = File.ReadAllText(filePath);
            _configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// Gets a configuration section
        /// </summary>
        /// <param name="key">Key of the section</param>
        /// <returns>The configuration section</returns>
        public IConfigurationSection GetSection(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var parts = key.Split(':');
            var current = _configuration;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (current.TryGetValue(parts[i], out var value) && value is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    current = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    return new JsonConfigurationSection(new Dictionary<string, object>());
                }
            }

            if (current.TryGetValue(parts[^1], out var sectionValue))
            {
                if (sectionValue is JsonElement sectionElement)
                {
                    if (sectionElement.ValueKind == JsonValueKind.Object)
                    {
                        var sectionDict = JsonSerializer.Deserialize<Dictionary<string, object>>(sectionElement.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return new JsonConfigurationSection(sectionDict);
                    }
                    else
                    {
                        return new JsonConfigurationSection(new Dictionary<string, object>
                        {
                            ["Value"] = sectionElement.GetRawText().Trim('"')
                        });
                    }
                }
                else if (sectionValue is Dictionary<string, object> sectionDict)
                {
                    return new JsonConfigurationSection(sectionDict);
                }
            }

            return new JsonConfigurationSection(new Dictionary<string, object>());
        }

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <returns>The configuration value</returns>
        public string GetValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var parts = key.Split(':');
            var current = _configuration;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (current.TryGetValue(parts[i], out var value) && value is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    current = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    return null;
                }
            }

            if (current.TryGetValue(parts[^1], out var valueObj))
            {
                if (valueObj is JsonElement valueElement)
                {
                    return valueElement.ValueKind switch
                    {
                        JsonValueKind.String => valueElement.GetString(),
                        JsonValueKind.Number => valueElement.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => null
                    };
                }

                return valueObj?.ToString();
            }

            return null;
        }
    }

    /// <summary>
    /// Simple JSON-based configuration section implementation
    /// </summary>
    public class JsonConfigurationSection : IConfigurationSection
    {
        private readonly Dictionary<string, object> _section;

        /// <summary>
        /// Initializes a new instance of the JsonConfigurationSection class
        /// </summary>
        /// <param name="section">Section data</param>
        public JsonConfigurationSection(Dictionary<string, object> section)
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
                if (value is JsonElement element)
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }

            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(_section), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// Gets child configuration sections
        /// </summary>
        /// <returns>Child configuration sections</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            foreach (var key in _section.Keys)
            {
                if (_section[key] is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    var childDict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    yield return new JsonConfigurationSection(childDict)
                    {
                        Key = key
                    };
                }
                else if (_section[key] is Dictionary<string, object> childDict)
                {
                    yield return new JsonConfigurationSection(childDict)
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
