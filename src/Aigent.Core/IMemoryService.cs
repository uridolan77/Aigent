using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Interface for memory services
    /// </summary>
    [System.Obsolete("Use Aigent.Core.Interfaces.IMemoryService instead. This interface is maintained for backward compatibility.")]
    public interface IMemoryService
    {
        /// <summary>
        /// Stores a value in memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key to store the value under</param>
        /// <param name="value">Value to store</param>
        /// <param name="durationType">Duration type (short, medium, long, permanent)</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task Store<T>(string key, T value, string durationType = "medium");
        
        /// <summary>
        /// Retrieves a value from memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key to retrieve the value for</param>
        /// <returns>The retrieved value, or default if not found</returns>
        Task<T> Retrieve<T>(string key);
        
        /// <summary>
        /// Stores context information in memory
        /// </summary>
        /// <typeparam name="T">Type of the context</typeparam>
        /// <param name="contextType">Type of the context</param>
        /// <param name="context">Context to store</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task StoreContext<T>(string contextType, T context);
        
        /// <summary>
        /// Retrieves context information from memory
        /// </summary>
        /// <typeparam name="T">Type of the context</typeparam>
        /// <param name="contextType">Type of the context</param>
        /// <returns>The retrieved context, or default if not found</returns>
        Task<T> RetrieveContext<T>(string contextType);
        
        /// <summary>
        /// Removes a value from memory
        /// </summary>
        /// <param name="key">Key to remove</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task Remove(string key);
        
        /// <summary>
        /// Clears all values from memory
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task Clear();
    }
}
