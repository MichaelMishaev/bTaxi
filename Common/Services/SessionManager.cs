using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Common.Services
{
    public class SessionManager
    {
        private readonly RedisCache _cache;

        public SessionManager(string redisConnectionString)
        {
            _cache = new RedisCache(redisConnectionString);
        }

        private string GenerateSessionKey(long userId, string key)
        {
            return $"{userId}:{key}";
        }

        public async Task SetSessionData<T>(string sessionKey, string key, T data)
        {
            string composedKey = $"{sessionKey}:{key}";
            await _cache.SetAsync(composedKey, data, TimeSpan.FromHours(1));
        }

        public async Task<T> GetSessionData<T>(string sessionKey, string key)
        {
            string composedKey = $"{sessionKey}:{key}";
            return await _cache.GetAsync<T>(composedKey);
        }

        public async Task RemoveSessionData(string sessionKey, string key)
        {
            string composedKey = $"{sessionKey}:{key}";
            await _cache.RemoveAsync(composedKey);
        }

        public async Task RemoveSessionData(long userId, string key)
        {
            string sessionKey = GenerateSessionKey(userId, key);
            await _cache.RemoveAsync(sessionKey);
        }

        public async Task SetSessionData<T>(long userId, string key, T data)
        {
            string sessionKey = GenerateSessionKey(userId, key);
            await _cache.SetAsync(sessionKey, data, TimeSpan.FromHours(1));
        }

        public async Task<T> GetSessionData<T>(long userId, string key)
        {
            string sessionKey = GenerateSessionKey(userId, key);
            return await _cache.GetAsync<T>(sessionKey);
        }

        public async Task SetDriverToClientMapping(long driverId, long clientChatId, long bidId)
        {
            string sessionKey = GenerateSessionKey(driverId, $"ClientChatId_{bidId}");
            await _cache.SetAsync(sessionKey, clientChatId, TimeSpan.FromHours(1));
        }

        public async Task<long?> GetClientChatIdForDriver(long driverId, long bidId)
        {
            string sessionKey = $"{driverId}:ClientChatId_{bidId}";
            return await GetSessionData<long?>(sessionKey, $"ClientChatId_{bidId}");
        }
    }
}
