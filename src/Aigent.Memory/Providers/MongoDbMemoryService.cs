using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Memory.Interfaces;
using Aigent.Monitoring;

namespace Aigent.Memory.Providers
{
    /// <summary>
    /// MongoDB-based implementation of ILongTermMemory
    /// </summary>
    public class MongoDbMemoryService : BaseMemoryService, ILongTermMemory
    {
        private readonly MongoDbProvider _mongoDbProvider;
        
        /// <summary>
        /// Initializes a new instance of the MongoDbMemoryService class
        /// </summary>
        /// <param name="connectionString">MongoDB connection string</param>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="collectionName">Name of the collection</param>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="logger">Logger for recording memory operations</param>
        /// <param name="metrics">Metrics collector for monitoring memory performance</param>
        public MongoDbMemoryService(
            string connectionString,
            string databaseName,
            string collectionName,
            string agentId,
            ILogger logger = null,
            IMetricsCollector metrics = null)
            : base(new MongoDbProvider(connectionString, databaseName, collectionName), agentId, logger, metrics)
        {
            _mongoDbProvider = (MongoDbProvider)MemoryProvider;
        }
        
        /// <summary>
        /// Ensures that all changes are persisted to storage
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task PersistAsync()
        {
            // MongoDB writes are already persisted, nothing to do here
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Stores a value with metadata
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="metadata">Additional metadata to store with the value</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task StoreWithMetadataAsync<T>(string key, T value, IDictionary<string, object> metadata, TimeSpan? expirationTime = null)
        {
            var prefixedKey = GetPrefixedKey(key);
            Logger?.Debug($"Storing value with key {prefixedKey} and metadata");
            
            await StoreAsync(key, value, expirationTime);
            
            var collection = (MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument>)_mongoDbProvider.GetStorageProvider();
            var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("key", prefixedKey);
            
            var update = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update;
            var updateDefinition = update.Set("metadata", MongoDB.Bson.BsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(metadata)));
            
            await collection.UpdateOneAsync(filter, updateDefinition);
            
            Logger?.Debug($"Stored metadata for key {prefixedKey}");
        }
        
        /// <summary>
        /// Retrieves metadata for a key
        /// </summary>
        /// <param name="key">Key to get metadata for</param>
        /// <returns>Dictionary of metadata, or null if the key doesn't exist</returns>
        public async Task<IDictionary<string, object>> GetMetadataAsync(string key)
        {
            var prefixedKey = GetPrefixedKey(key);
            Logger?.Debug($"Retrieving metadata for key {prefixedKey}");
            
            var collection = (MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument>)_mongoDbProvider.GetStorageProvider();
            var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("key", prefixedKey);
            var projection = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Projection.Include("metadata");
            
            var document = await collection.Find(filter).Project(projection).FirstOrDefaultAsync();
            
            if (document == null || !document.Contains("metadata"))
            {
                return null;
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(document["metadata"].ToJson());
        }
        
        /// <summary>
        /// Searches memory for values matching a query
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>List of keys that match the query</returns>
        public async Task<IEnumerable<string>> SearchAsync(string query, int limit = 10)
        {
            Logger?.Debug($"Searching with query: {query}, limit: {limit}");
            
            var collection = (MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument>)_mongoDbProvider.GetStorageProvider();
            
            // Create text search filter
            var textFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Text(query);
            var agentFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Regex("key", new MongoDB.Bson.BsonRegularExpression($"^{AgentId}:", "i"));
            var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.And(textFilter, agentFilter);
            
            var projection = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Projection.Include("key");
            var sort = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Sort.Descending("score", "lastAccessedAt");
            
            var documents = await collection.Find(filter)
                .Project(projection)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();
            
            var keys = documents.Select(d => d["key"].AsString).ToList();
            return RemovePrefixFromKeys(keys);
        }
        
        /// <summary>
        /// Flushes any pending changes to persistent storage
        /// </summary>
        public override Task Flush()
        {
            return PersistAsync();
        }
    }
}
