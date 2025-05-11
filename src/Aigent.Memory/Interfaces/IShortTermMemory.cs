using System;
using System.Threading.Tasks;

namespace Aigent.Memory.Interfaces
{
    /// <summary>
    /// Interface for short-term memory services that cache frequently accessed data
    /// </summary>
    public interface IShortTermMemory : IMemoryService
    {
        /// <summary>
        /// Sets the default expiration time for items stored in short-term memory
        /// </summary>
        /// <param name="expirationTime">The default expiration time</param>
        void SetDefaultExpirationTime(TimeSpan expirationTime);
        
        /// <summary>
        /// Renews the expiration time for a key
        /// </summary>
        /// <param name="key">Key to renew</param>
        /// <param name="expirationTime">New expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RenewExpirationAsync(string key, TimeSpan expirationTime);
        
        /// <summary>
        /// Gets the remaining time until a key expires
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>The remaining time, or null if the key doesn't exist or has no expiration</returns>
        Task<TimeSpan?> GetExpirationTimeRemainingAsync(string key);
    }
}
