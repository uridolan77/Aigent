using System;
using System.Collections.Generic;

namespace Aigent.Memory.Models
{
    /// <summary>
    /// Represents an entry in memory with metadata
    /// </summary>
    public class MemoryEntry
    {
        /// <summary>
        /// Gets or sets the key for the entry
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// Gets or sets the value of the entry
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the value
        /// </summary>
        public Type ValueType { get; set; }
        
        /// <summary>
        /// Gets or sets the time when the entry was created
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the time when the entry was last accessed
        /// </summary>
        public DateTimeOffset LastAccessedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the time when the entry expires
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata for the entry
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the MemoryEntry class
        /// </summary>
        public MemoryEntry()
        {
            CreatedAt = DateTimeOffset.UtcNow;
            LastAccessedAt = DateTimeOffset.UtcNow;
            Metadata = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Initializes a new instance of the MemoryEntry class with specified values
        /// </summary>
        /// <param name="key">Key for the entry</param>
        /// <param name="value">Value of the entry</param>
        /// <param name="valueType">Type of the value</param>
        /// <param name="expirationTime">Optional expiration time</param>
        public MemoryEntry(string key, object value, Type valueType, TimeSpan? expirationTime = null)
        {
            Key = key;
            Value = value;
            ValueType = valueType;
            CreatedAt = DateTimeOffset.UtcNow;
            LastAccessedAt = DateTimeOffset.UtcNow;
            
            if (expirationTime.HasValue)
            {
                ExpiresAt = DateTimeOffset.UtcNow.Add(expirationTime.Value);
            }
            
            Metadata = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Checks if the entry has expired
        /// </summary>
        /// <returns>True if the entry has expired, false otherwise</returns>
        public bool IsExpired()
        {
            return ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// Updates the last accessed time
        /// </summary>
        public void UpdateLastAccessed()
        {
            LastAccessedAt = DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// Sets the expiration time
        /// </summary>
        /// <param name="expirationTime">New expiration time</param>
        public void SetExpiration(TimeSpan expirationTime)
        {
            ExpiresAt = DateTimeOffset.UtcNow.Add(expirationTime);
        }
    }
}
