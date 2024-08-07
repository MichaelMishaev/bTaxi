using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Services
{
    //############### comments: Added a SessionManager class with a focus on session management
    public static class SessionManager
    {
        private static readonly ISessionStorage _sessionStorage = new InMemorySessionStorage(); //############### comments: Added a default session storage implementation

        //############### comments: Made method static to access the storage interface
        public static void SetSessionData<T>(long userId, string key, T data)
        {
            var sessionKey = GenerateSessionKey(userId, key);
            var jsonData = JsonConvert.SerializeObject(data);
            _sessionStorage.Set(sessionKey, jsonData);
        }

        //############### comments: Added method to set driver-to-client mapping
        public static void SetDriverToClientMapping(long driverId, long clientChatId, long bidId)
        {
            SetSessionData(driverId, $"ClientChatId_{bidId}", clientChatId);
        }

        //############### comments: Added method to get client chat ID for driver
        public static long? GetClientChatIdForDriver(long driverId, long bidId)
        {
            return GetSessionData<long?>(driverId, $"ClientChatId_{bidId}");
        }

        //############### comments: Made method static to access the storage interface
        public static T GetSessionData<T>(long userId, string key)
        {
            var sessionKey = GenerateSessionKey(userId, key);
            var jsonData = _sessionStorage.Get(sessionKey);
            return jsonData == null ? default(T) : JsonConvert.DeserializeObject<T>(jsonData);
        }

        //############### comments: Made method static to access the storage interface
        public static void RemoveSessionData(long userId, string key)
        {
            var sessionKey = GenerateSessionKey(userId, key);
            _sessionStorage.Remove(sessionKey);
        }

        private static string GenerateSessionKey(long userId, string key)
        {
            return $"{userId}:{key}";
        }
    }

    //############### comments: Created interface for session storage
    public interface ISessionStorage
    {
        void Set(string key, string value);
        string Get(string key);
        void Remove(string key);
    }

    //############### comments: Created in-memory implementation of session storage
    public class InMemorySessionStorage : ISessionStorage
    {
        private readonly Dictionary<string, string> _sessionData = new Dictionary<string, string>();

        public void Set(string key, string value)
        {
            _sessionData[key] = value;
        }

        public string Get(string key)
        {
            _sessionData.TryGetValue(key, out var value);
            return value;
        }

        public void Remove(string key)
        {
            _sessionData.Remove(key);
        }
    }
}
