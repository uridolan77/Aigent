using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Memory.Interfaces
{
    /// <summary>
    /// Interface for memory providers that handle different storage backends
    /// </summary>
    public interface IMemoryProvider
    {
        /// <summary>
        /// Initializes the memory provider
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task InitializeAsync();
        
        /// <summary>
        /// Stores a value in memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task StoreAsync<T>(string key, T value, TimeSpan? expirationTime = null);
        
        /// <summary>
        /// Retrieves a value from memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <returns>The stored value, or default if not found</returns>
        Task<T> RetrieveAsync<T>(string key);
        
        /// <summary>
        /// Checks if a key exists in memory
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        Task<bool> ExistsAsync(string key);
        
        /// <summary>
        /// Removes a value from memory
        /// </summary>
        /// <param name="key">Key for the value to remove</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RemoveAsync(string key);
        
        /// <summary>
        /// Gets all keys in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        Task<IEnumerable<string>> GetKeysAsync(string pattern = "*");
        
        /// <summary>
        /// Gets all key-value pairs in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>Dictionary of key-value pairs</returns>
        Task<IDictionary<string, object>> GetAllAsync(string pattern = "*");
        
        /// <summary>
        /// Clears all memory for the current context
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ClearAsync();
        
        /// <summary>
        /// Gets the underlying storage provider for direct access if needed
        /// </summary>
        /// <returns>The underlying storage provider</returns>
        object GetStorageProvider();
    }
}
