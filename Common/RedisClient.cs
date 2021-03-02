using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class RedisClient : ICacheDataSource
    {        
        private readonly ILogger _logger;
        private readonly IDatabaseAsync _database;
        private int expiry = 15;

        public RedisClient(IDatabaseAsync database, ILogger logger)
        {
            _database = database;
            _logger = logger;            
        }       

        public async Task<T> Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            try
            {
                _logger.LogInformation($"### Aeronology.DTO.Models.RedisClient.Get('{key}') ###");
               
                var value = await _database.StringGetAsync(key);                    

                if (value.IsNullOrEmpty)
                {
                    Console.WriteLine($"### Aeronology.DTO.Models.RedisClient.Get('{key}') => empty ###");
                    return default;
                }
                    
                return JsonSerializer.Deserialize<T>(value.ToString());                    
                                         
            }
            catch (Exception ex)
            {
                _logger.LogError($"### Aeronology.DTO.Models.RedisClient.Get('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
                return default;
            }
        }        

        public async Task Set<T>(string key, T value)
        {
            Console.WriteLine($"### REDIS Set('{key}') ###");

            try
            {
                await _database.StringSetAsync(key,JsonSerializer.Serialize(value), TimeSpan.FromMinutes(expiry));
            }
            catch (Exception ex)
            {
                _logger.LogError($"### REDIS Set('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
                throw new AeronologyException("CACHE_INSERT_FAIL", "Error inserting a record to cache");
            }
        }

        public async Task Delete(string key)
        {
            _logger.LogInformation($"### REDIS Delete('{key}') ###");

            try
            {
                await _database.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError($"### REDIS Delete('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
                throw new AeronologyException("CACHE_DELETE_FAIL", "Error deleting a record to cache");
            }
        }

        public async Task ListRightPushAsync<T>(string key, T value)
        {
            _logger.LogInformation($"### REDIS ListRightPushAsync('{key}') ###");

            try
            {
                await _database.ListRightPushAsync(key, JsonSerializer.Serialize(value));            
            }
            catch (Exception ex)
            {
                _logger.LogError($"### REDIS ListRightPushAsync('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
                throw new AeronologyException("CACHE_INSERT_FAIL", "Error inserting a record to cache");
            }
        }

        public async Task<long> ListLengthAsync(string key)
        {
            _logger.LogInformation($"### ListLengthAsync('{key}') ###");

            try
            {
                return await _database.ListLengthAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError($"### ListLengthAsync('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
                return default;
            }
        }

        public async Task<List<T>> ListRangeAsync<T>(string key)
        {
            _logger.LogInformation($"### ListRangeAsync('{key}') ###");

            try
            {
                var items = await _database.ListRangeAsync(key);

                if(items.Length == 0)
                {
                    return default;
                }

                return items.Select(x =>
                {
                    return JsonSerializer.Deserialize<T>(x);
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"### ListRangeAsync('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
                return default;
            }
        }

        public async Task ListRemoveAtAsync(string key, int index)
        {
            _logger.LogInformation($"### ListRemoveAtAsync('{key}') ###");

            try
            {
                var value = await _database.ListGetByIndexAsync(key, index);
                if (!value.IsNullOrEmpty)
                {
                    await _database.ListRemoveAsync(key, value);
                    _logger.LogInformation($"Successfully removed session {value} at index {index}.");
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError($"### ListRemoveAtAsync('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
            }
        }

    }
}
