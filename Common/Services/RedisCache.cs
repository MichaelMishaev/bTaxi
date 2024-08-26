using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace Common.Services
{
    public class RedisCache
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public RedisCache(string redisConnectionString)
        {
            _redis = ConnectionMultiplexer.Connect(redisConnectionString);
            _database = _redis.GetDatabase();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var jsonData = JsonConvert.SerializeObject(value);
            await _database.StringSetAsync(key, jsonData, expiration);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var jsonData = await _database.StringGetAsync(key);
            if (jsonData.IsNullOrEmpty) return default(T);
            return JsonConvert.DeserializeObject<T>(jsonData);
        }

        public async Task RemoveAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }
    }
}
