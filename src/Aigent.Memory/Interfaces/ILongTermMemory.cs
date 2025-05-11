using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aigent.Memory.Interfaces
{
    /// <summary>
    /// Interface for long-term memory services that provide persistent storage
    /// </summary>
    public interface ILongTermMemory : IMemoryService
    {
        /// <summary>
        /// Ensures that all changes are persisted to storage
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task PersistAsync();
        
        /// <summary>
        /// Stores a value with metadata
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="metadata">Additional metadata to store with the value</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task StoreWithMetadataAsync<T>(string key, T value, IDictionary<string, object> metadata, TimeSpan? expirationTime = null);
        
        /// <summary>
        /// Retrieves metadata for a key
        /// </summary>
        /// <param name="key">Key to get metadata for</param>
        /// <returns>Dictionary of metadata, or null if the key doesn't exist</returns>
        Task<IDictionary<string, object>> GetMetadataAsync(string key);
        
        /// <summary>
        /// Searches memory for values matching a query
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>List of keys that match the query</returns>
        Task<IEnumerable<string>> SearchAsync(string query, int limit = 10);
    }
}
