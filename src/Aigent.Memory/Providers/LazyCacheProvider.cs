using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LazyCache;
using Aigent.Memory.Interfaces;

namespace Aigent.Memory.Providers
{
    /// <summary>
    /// LazyCache-based implementation of IMemoryProvider
    /// </summary>
    public class LazyCacheProvider : IMemoryProvider
    {
        private readonly IAppCache _cache;
        private readonly Dictionary<string, Type> _typeRegistry = new();
        private readonly Dictionary<string, TimeSpan?> _expirationTimes = new();
        
        /// <summary>
        /// Initializes a new instance of the LazyCacheProvider class
        /// </summary>
        /// <param name="cache">LazyCache instance</param>
        public LazyCacheProvider(IAppCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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
            if (expirationTime.HasValue)
            {
                _cache.Add(key, value, expirationTime.Value);
            }
            else
            {
                _cache.Add(key, value);
            }
            
            // Keep track of the type and expiration for this key
            _typeRegistry[key] = typeof(T);
            _expirationTimes[key] = expirationTime;
            
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
            return Task.FromResult(_cache.Get<T>(key));
        }
        
        /// <summary>
        /// Checks if a key exists in memory
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public Task<bool> ExistsAsync(string key)
        {
            var result = _typeRegistry.ContainsKey(key);
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Removes a value from memory
        /// </summary>
        /// <param name="key">Key for the value to remove</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            _typeRegistry.Remove(key);
            _expirationTimes.Remove(key);
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets all keys in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        public Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
        {
            var regex = CreateRegexFromGlob(pattern);
            var keys = _typeRegistry.Keys.Where(k => regex.IsMatch(k));
            
            return Task.FromResult(keys);
        }
        
        /// <summary>
        /// Gets all key-value pairs in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>Dictionary of key-value pairs</returns>
        public async Task<IDictionary<string, object>> GetAllAsync(string pattern = "*")
        {
            var keys = await GetKeysAsync(pattern);
            var result = new Dictionary<string, object>();
            
            foreach (var key in keys)
            {
                var type = _typeRegistry[key];
                var methodInfo = typeof(LazyCacheProvider).GetMethod("RetrieveAsync");
                var genericMethod = methodInfo.MakeGenericMethod(type);
                
                var taskResult = genericMethod.Invoke(this, new object[] { key });
                var resultProperty = taskResult.GetType().GetProperty("Result");
                var value = resultProperty.GetValue(taskResult);
                
                result[key] = value;
            }
            
            return result;
        }
        
        /// <summary>
        /// Clears all memory
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task ClearAsync()
        {
            foreach (var key in _typeRegistry.Keys.ToList())
            {
                _cache.Remove(key);
            }
            
            _typeRegistry.Clear();
            _expirationTimes.Clear();
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets the underlying storage provider
        /// </summary>
        /// <returns>The LazyCache instance</returns>
        public object GetStorageProvider()
        {
            return _cache;
        }
        
        // Helper methods
        
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
