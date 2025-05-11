using System;

namespace Aigent.Memory
{
    /// <summary>
    /// Options for configuring memory services
    /// </summary>
    public class MemoryServiceOptions
    {
        /// <summary>
        /// Gets or sets the type of memory service to create
        /// </summary>
        public MemoryServiceType ServiceType { get; set; } = MemoryServiceType.InMemory;
        
        /// <summary>
        /// Gets or sets the connection string for persistent storage
        /// </summary>
        public string ConnectionString { get; set; }
        
        /// <summary>
        /// Gets or sets the default expiration time for items in memory
        /// </summary>
        public TimeSpan? DefaultExpirationTime { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum capacity for in-memory providers
        /// </summary>
        public int? MaxCapacity { get; set; }
        
        /// <summary>
        /// Gets or sets the database name for database providers
        /// </summary>
        public string DatabaseName { get; set; }
        
        /// <summary>
        /// Gets or sets the collection or table name for database providers
        /// </summary>
        public string CollectionName { get; set; }
        
        /// <summary>
        /// Gets or sets whether to enable distributed caching
        /// </summary>
        public bool EnableDistributedCache { get; set; }
        
        /// <summary>
        /// Gets or sets the prefix for keys in storage
        /// </summary>
        public string KeyPrefix { get; set; }
    }
    
    /// <summary>
    /// Types of memory services
    /// </summary>
    public enum MemoryServiceType
    {
        /// <summary>
        /// In-memory storage
        /// </summary>
        InMemory,
        
        /// <summary>
        /// LazyCache-based storage
        /// </summary>
        LazyCache,
        
        /// <summary>
        /// MongoDB-based storage
        /// </summary>
        MongoDB,
        
        /// <summary>
        /// Redis-based storage
        /// </summary>
        Redis,
        
        /// <summary>
        /// SQL-based storage
        /// </summary>
        SqlServer
    }
}
