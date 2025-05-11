using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Aigent.Memory.Interfaces;

namespace Aigent.Memory.Providers
{
    /// <summary>
    /// MongoDB-based implementation of IMemoryProvider
    /// </summary>
    public class MongoDbProvider : IMemoryProvider
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;
        
        /// <summary>
        /// Initializes a new instance of the MongoDbProvider class
        /// </summary>
        /// <param name="connectionString">MongoDB connection string</param>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="collectionName">Name of the collection</param>
        public MongoDbProvider(string connectionString, string databaseName, string collectionName)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));
            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentNullException(nameof(collectionName));
            
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
            _collection = _database.GetCollection<BsonDocument>(collectionName);
        }
        
        /// <summary>
        /// Initializes the memory provider
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            // Create indexes if they don't exist
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("key");
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<BsonDocument>(indexKeys, indexOptions);
            
            await _collection.Indexes.CreateOneAsync(indexModel);
            
            // Create TTL index for expiration
            var ttlIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("expiresAt");
            var ttlIndexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            var ttlIndexModel = new CreateIndexModel<BsonDocument>(ttlIndexKeys, ttlIndexOptions);
            
            await _collection.Indexes.CreateOneAsync(ttlIndexModel);
        }
        
        /// <summary>
        /// Stores a value in memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <param name="value">Value to store</param>
        /// <param name="expirationTime">Optional expiration time</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task StoreAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var typeName = typeof(T).AssemblyQualifiedName;
            
            var document = new BsonDocument
            {
                { "key", key },
                { "value", serializedValue },
                { "valueType", typeName },
                { "createdAt", DateTime.UtcNow },
                { "lastAccessedAt", DateTime.UtcNow }
            };
            
            if (expirationTime.HasValue)
            {
                document["expiresAt"] = DateTime.UtcNow.Add(expirationTime.Value);
            }
            
            var filter = Builders<BsonDocument>.Filter.Eq("key", key);
            var options = new ReplaceOptions { IsUpsert = true };
            
            await _collection.ReplaceOneAsync(filter, document, options);
        }
        
        /// <summary>
        /// Retrieves a value from memory
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key for the value</param>
        /// <returns>The stored value, or default if not found</returns>
        public async Task<T> RetrieveAsync<T>(string key)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("key", key);
            var document = await _collection.Find(filter).FirstOrDefaultAsync();
            
            if (document == null)
            {
                return default;
            }
            
            // Update last accessed time
            var update = Builders<BsonDocument>.Update.Set("lastAccessedAt", DateTime.UtcNow);
            await _collection.UpdateOneAsync(filter, update);
            
            var serializedValue = document["value"].AsString;
            return JsonSerializer.Deserialize<T>(serializedValue);
        }
        
        /// <summary>
        /// Checks if a key exists in memory
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public async Task<bool> ExistsAsync(string key)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("key", key);
            var count = await _collection.CountDocumentsAsync(filter);
            return count > 0;
        }
        
        /// <summary>
        /// Removes a value from memory
        /// </summary>
        /// <param name="key">Key for the value to remove</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task RemoveAsync(string key)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("key", key);
            await _collection.DeleteOneAsync(filter);
        }
        
        /// <summary>
        /// Gets all keys in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>List of keys</returns>
        public async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
        {
            var regex = CreateMongoRegexFromGlob(pattern);
            var filter = Builders<BsonDocument>.Filter.Regex("key", regex);
            
            var projection = Builders<BsonDocument>.Projection.Include("key");
            var documents = await _collection.Find(filter).Project(projection).ToListAsync();
            
            return documents.Select(d => d["key"].AsString);
        }
        
        /// <summary>
        /// Gets all key-value pairs in memory
        /// </summary>
        /// <param name="pattern">Optional pattern for filtering keys</param>
        /// <returns>Dictionary of key-value pairs</returns>
        public async Task<IDictionary<string, object>> GetAllAsync(string pattern = "*")
        {
            var regex = CreateMongoRegexFromGlob(pattern);
            var filter = Builders<BsonDocument>.Filter.Regex("key", regex);
            
            var documents = await _collection.Find(filter).ToListAsync();
            var result = new Dictionary<string, object>();
            
            foreach (var document in documents)
            {
                var key = document["key"].AsString;
                var serializedValue = document["value"].AsString;
                var typeName = document["valueType"].AsString;
                
                var type = Type.GetType(typeName);
                if (type != null)
                {
                    var value = JsonSerializer.Deserialize(serializedValue, type);
                    result[key] = value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Clears all memory
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ClearAsync()
        {
            await _collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        }
        
        /// <summary>
        /// Gets the underlying storage provider
        /// </summary>
        /// <returns>The MongoDB collection</returns>
        public object GetStorageProvider()
        {
            return _collection;
        }
        
        // Helper methods
        
        private static BsonRegularExpression CreateMongoRegexFromGlob(string pattern)
        {
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                .Replace("\\[", "[")
                .Replace("\\]", "]") + "$";
            
            return new BsonRegularExpression(regexPattern, "i");
        }
    }
}
