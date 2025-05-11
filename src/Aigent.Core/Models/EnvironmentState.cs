using System;
using System.Collections.Generic;

namespace Aigent.Core.Models
{
    /// <summary>
    /// Represents the current state of the environment in which an agent operates
    /// </summary>
    public class EnvironmentState
    {
        /// <summary>
        /// Initializes a new instance of the EnvironmentState class
        /// </summary>
        public EnvironmentState()
        {
            Properties = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Gets or sets the ID of the state
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the timestamp of the state
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the properties of the state
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }
        
        /// <summary>
        /// Gets or sets the previous state
        /// </summary>
        public EnvironmentState PreviousState { get; set; }
        
        /// <summary>
        /// Gets a property value
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="defaultValue">Default value if property not found</param>
        /// <returns>Property value</returns>
        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (Properties.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                
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
        /// Sets a property value
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        public void SetProperty<T>(string key, T value)
        {
            Properties[key] = value;
        }
        
        /// <summary>
        /// Checks if a property exists
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>True if the property exists, false otherwise</returns>
        public bool HasProperty(string key)
        {
            return Properties.ContainsKey(key);
        }
        
        /// <summary>
        /// Creates a clone of the current state
        /// </summary>
        /// <returns>Clone of the current state</returns>
        public EnvironmentState Clone()
        {
            var clone = new EnvironmentState
            {
                Id = Id,
                Timestamp = Timestamp,
                Properties = new Dictionary<string, object>(Properties),
                PreviousState = PreviousState
            };
            
            return clone;
        }
    }
}
