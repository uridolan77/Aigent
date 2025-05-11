using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aigent.Memory.Interfaces;
using Aigent.Memory.Models;

namespace Aigent.Memory.Providers
{
    /// <summary>
    /// Thread-safe in-memory implementation of IMemoryProvider
    /// </summary>
    public class ConcurrentMemoryProvider : IMemoryProvider
    {
        private readonly ConcurrentDictionary<string, MemoryEntry> _memory = new();
        private readonly int? _maxCapacity;
        
        /// <summary>
        /// Initializes a new instance of the ConcurrentMemoryProvider class
        /// </summary>
        /// <param name="maxCapacity">Optional maximum capacity</param>
        public ConcurrentMemoryProvider(int? maxCapacity = null)
        {
            _maxCapacity = maxCapacity;
        }
        
        /// <summary>
        /// Initializes the memory provider
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Stores a value in memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task StoreAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var entry = new MemoryEntry(key, value, typeof(T), expirationTime);
            
            // If at capacity, remove expired or oldest items
            if (_maxCapacity.HasValue && _memory.Count >= _maxCapacity.Value)
            {
                RemoveExpiredEntries();
                
                // If still at capacity, remove oldest entry
                if (_memory.Count >= _maxCapacity.Value)
                {
                    var oldest = _memory.OrderBy(x => x.Value.LastAccessedAt).FirstOrDefault();
                    _memory.TryRemove(oldest.Key, out _);
                }
            }
            
            _memory[key] = entry;
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Retrieves a value from memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <returns>The stored value, or default if not found</returns>
        public Task<T> RetrieveAsync<T>(string key)
        {
            if (_memory.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired())
                {
                    _memory.TryRemove(key, out _);
                    return Task.FromResult<T>(default);
                }
                
                entry.UpdateLastAccessed();
                return Task.FromResult((T)entry.Value);
            }
            
            return Task.FromResult<T>(default);
        }
        
        /// <summary>
        /// Checks if a key exists in memory
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public Task<bool> ExistsAsync(string key)
        {
            if (_memory.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired())
                {
                    _memory.TryRemove(key, out _);
                    return Task.FromResult(false);
                }
                
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
        
        /// <summary>
        /// Removes a value from memory
        /// </summary>
        /// <param name="key">Key for the value to remove</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task RemoveAsync(string key)
        {
            _memory.TryRemove(key, out _);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets all keys in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        public Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
        {
            RemoveExpiredEntries();
            
            var regex = CreateRegexFromGlob(pattern);
            var keys = _memory.Keys.Where(k => regex.IsMatch(k));
            
            return Task.FromResult(keys);
        }
        
        /// <summary>
        /// Gets all key-value pairs in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>Dictionary of key-value pairs</returns>
        public Task<IDictionary<string, object>> GetAllAsync(string pattern = "*")
        {
            RemoveExpiredEntries();
            
            var regex = CreateRegexFromGlob(pattern);
            var result = new Dictionary<string, object>();
            
            foreach (var pair in _memory)
            {
                if (regex.IsMatch(pair.Key))
                {
                    result[pair.Key] = pair.Value.Value;
                }
            }
            
            return Task.FromResult<IDictionary<string, object>>(result);
        }
        
        /// <summary>
        /// Clears all memory
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task ClearAsync()
        {
            _memory.Clear();
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets the underlying storage provider
        /// </summary>
        /// <returns>The concurrent dictionary storing memory entries</returns>
        public object GetStorageProvider()
        {
            return _memory;
        }
        
        // Helper methods
        
        private void RemoveExpiredEntries()
        {
            var expiredKeys = _memory.Where(pair => pair.Value.IsExpired()).Select(pair => pair.Key).ToList();
            
            foreach (var key in expiredKeys)
            {
                _memory.TryRemove(key, out _);
            }
        }
        
        private static Regex CreateRegexFromGlob(string pattern)
        {
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                .Replace("\\[", "[")
                .Replace("\\]", "]") + "$";
            
            return new Regex(regexPattern, RegexOptions.IgnoreCase);
        }
    }
}
