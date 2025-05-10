using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Aigent.Monitoring;

namespace Aigent.Memory
{
    /// <summary>
    /// MongoDB-based implementation of ILongTermMemory
    /// </summary>
    public class DocumentDbMemoryService : ILongTermMemory
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;
        private string _agentId;

        /// <summary>
        /// Initializes a new instance of the DocumentDbMemoryService class
        /// </summary>
        /// <param name="connectionString">MongoDB connection string</param>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="collectionName">Name of the collection</param>
        /// <param name="logger">Logger for recording memory operations</param>
        /// <param name="metrics">Metrics collector for monitoring memory performance</param>
        public DocumentDbMemoryService(
            string connectionString,
            string databaseName = "AigentMemory",
            string collectionName = "AgentContext",
            ILogger logger = null,
            IMetricsCollector metrics = null)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _logger = logger;
            _metrics = metrics;

            try
            {
                _client = new MongoClient(connectionString);
                _database = _client.GetDatabase(databaseName);
                _collection = _database.GetCollection<BsonDocument>(collectionName);
                
                _logger?.Log(LogLevel.Information, $"Connected to MongoDB: {databaseName}.{collectionName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to connect to MongoDB: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Initializes the memory service for a specific agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        public async Task Initialize(string agentId)
        {
            _agentId = agentId;
            
            // Create index on agent ID and key for faster lookups
            var indexKeysDefinition = Builders<BsonDocument>.IndexKeys
                .Ascending("AgentId")
                .Ascending("Key");
            
            var indexOptions = new CreateIndexOptions { Name = "AgentId_Key_Index" };
            var indexModel = new CreateIndexModel<BsonDocument>(indexKeysDefinition, indexOptions);
            
            await _collection.Indexes.CreateOneAsync(indexModel);
            
            _logger?.Log(LogLevel.Debug, $"Initialized MongoDB memory service for agent {agentId}");
        }

        /// <summary>
        /// Stores a value in the agent's context
        /// </summary>
        /// <param name="key">Key to store the value under</param>
        /// <param name="value">Value to store</param>
        /// <param name="ttl">Optional time-to-live for the value</param>
        public async Task StoreContext(string key, object value, TimeSpan? ttl = null)
        {
            _metrics?.StartOperation($"memory_{_agentId}_store");
            
            try
            {
                var fullKey = $"{_agentId}:{key}";
                var serialized = JsonSerializer.Serialize(value);
                
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("AgentId", _agentId),
                    Builders<BsonDocument>.Filter.Eq("Key", key));
                
                var expiry = ttl.HasValue ? DateTime.UtcNow + ttl.Value : (DateTime?)null;
                
                var document = new BsonDocument
                {
                    { "AgentId", _agentId },
                    { "Key", key },
                    { "Value", serialized },
                    { "Expiry", expiry?.ToString("o") ?? BsonNull.Value },
                    { "LastUpdated", DateTime.UtcNow.ToString("o") }
                };
                
                var options = new ReplaceOptions { IsUpsert = true };
                await _collection.ReplaceOneAsync(filter, document, options);
                
                _logger?.Log(LogLevel.Debug, $"Stored value for key {fullKey} in MongoDB");
                _metrics?.RecordMetric($"memory.{_agentId}.store_count", 1.0);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error storing context in MongoDB: {ex.Message}", ex);
                _metrics?.RecordMetric($"memory.{_agentId}.store_error_count", 1.0);
                throw;
            }
            finally
            {
                _metrics?.EndOperation($"memory_{_agentId}_store");
            }
        }

        /// <summary>
        /// Retrieves a value from the agent's context
        /// </summary>
        /// <typeparam name="T">Type of the value to retrieve</typeparam>
        /// <param name="key">Key the value is stored under</param>
        /// <returns>The retrieved value, or default if not found</returns>
        public async Task<T> RetrieveContext<T>(string key)
        {
            _metrics?.StartOperation($"memory_{_agentId}_retrieve");
            
            try
            {
                var fullKey = $"{_agentId}:{key}";
                
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("AgentId", _agentId),
                    Builders<BsonDocument>.Filter.Eq("Key", key),
                    Builders<BsonDocument>.Filter.Or(
                        Builders<BsonDocument>.Filter.Eq("Expiry", BsonNull.Value),
                        Builders<BsonDocument>.Filter.Gt("Expiry", DateTime.UtcNow.ToString("o"))
                    ));
                
                var document = await _collection.Find(filter).FirstOrDefaultAsync();
                
                if (document != null && document.Contains("Value"))
                {
                    var serialized = document["Value"].AsString;
                    var result = JsonSerializer.Deserialize<T>(serialized);
                    
                    _logger?.Log(LogLevel.Debug, $"Retrieved value for key {fullKey} from MongoDB");
                    _metrics?.RecordMetric($"memory.{_agentId}.retrieve_hit_count", 1.0);
                    
                    return result;
                }
                
                _logger?.Log(LogLevel.Debug, $"No value found for key {fullKey} in MongoDB");
                _metrics?.RecordMetric($"memory.{_agentId}.retrieve_miss_count", 1.0);
                
                return default;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving context from MongoDB: {ex.Message}", ex);
                _metrics?.RecordMetric($"memory.{_agentId}.retrieve_error_count", 1.0);
                throw;
            }
            finally
            {
                _metrics?.EndOperation($"memory_{_agentId}_retrieve");
            }
        }

        /// <summary>
        /// Clears all memory for the agent
        /// </summary>
        public async Task ClearMemory()
        {
            _metrics?.StartOperation($"memory_{_agentId}_clear");
            
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("AgentId", _agentId);
                var result = await _collection.DeleteManyAsync(filter);
                
                _logger?.Log(LogLevel.Information, $"Cleared memory for agent {_agentId} from MongoDB, deleted {result.DeletedCount} documents");
                _metrics?.RecordMetric($"memory.{_agentId}.clear_count", 1.0);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error clearing memory from MongoDB: {ex.Message}", ex);
                _metrics?.RecordMetric($"memory.{_agentId}.clear_error_count", 1.0);
                throw;
            }
            finally
            {
                _metrics?.EndOperation($"memory_{_agentId}_clear");
            }
        }

        /// <summary>
        /// Flushes any pending changes and cleans up expired entries
        /// </summary>
        public async Task Flush()
        {
            _metrics?.StartOperation($"memory_{_agentId}_flush");
            
            try
            {
                // Remove expired entries
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Ne("Expiry", BsonNull.Value),
                    Builders<BsonDocument>.Filter.Lt("Expiry", DateTime.UtcNow.ToString("o")));
                
                var result = await _collection.DeleteManyAsync(filter);
                
                _logger?.Log(LogLevel.Debug, $"Flushed memory for agent {_agentId}, removed {result.DeletedCount} expired entries from MongoDB");
                _metrics?.RecordMetric($"memory.{_agentId}.expired_entries_count", result.DeletedCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error flushing memory in MongoDB: {ex.Message}", ex);
                _metrics?.RecordMetric($"memory.{_agentId}.flush_error_count", 1.0);
                throw;
            }
            finally
            {
                _metrics?.EndOperation($"memory_{_agentId}_flush");
            }
        }
    }
}
