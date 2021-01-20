using System;
using System.Text.Json;
using System.Threading.Tasks;
using SabreWebtopTicketingService.Services;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using SabreWebtopTicketingService.Models;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace SabreWebtopTicketingService.Common
{
    public class DbCache
    {
        private readonly string CACHE_DB = $"{Environment.GetEnvironmentVariable("ENVIRONMENT")??"dev"}-sabre-session";
        private readonly SessionRefreshService _sessionRefreshService;
        private readonly ILogger _logger;
        private readonly Table table;
        private int expiry = 15;

        public DbCache(IAmazonDynamoDB dynamoDbClient, SessionRefreshService sessionRefreshService, ILogger logger)
        {
            table = Table.LoadTable(dynamoDbClient, CACHE_DB);
            _sessionRefreshService = sessionRefreshService;
            _logger = logger;
        }

        public async Task<List<SabreSession>> ListSabreSessions(string pccKey, string fieldValue)
        {
            var queryFilter = new QueryFilter("pcc_key", QueryOperator.Equal, new List<AttributeValue>() { new AttributeValue(pccKey) });
            var queryConfig = new QueryOperationConfig()
            {
                IndexName = "pcc_key-index",
                Filter = queryFilter
            };
            var searchResult = table.Query(queryConfig);

            var sabreSessions = new List<SabreSession>();

            if (searchResult.Count == 0) { return sabreSessions; }

            do
            {
                var docSet = await searchResult.GetNextSetAsync();
                docSet.ForEach(doc =>
                {
                    var sabreSession = JsonSerializer.Deserialize<SabreSession>(doc[fieldValue]);
                    sabreSessions.Add(sabreSession);
                });
            }
            while (!searchResult.IsDone);

            return sabreSessions;
        }

        public async Task<int> SabreSessionCount(string pccKey)
        {
            try
            {
                var queryFilter = new QueryFilter("pcc_key", QueryOperator.Equal, new List<AttributeValue>() { new AttributeValue(pccKey) });
                var queryConfig = new QueryOperationConfig()
                {
                    IndexName = "pcc_key-index",
                    Filter = queryFilter
                };
                var searchResult = table.Query(queryConfig);

                return searchResult.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured counting sabre sessions in cache. {ErrorMessage} {Stacktrace}.", ex.Message, ex.StackTrace);
                return 0;
            }
        }

        public async Task<T> Get<T>(string cacheKey, string fieldValue)
        {
            return await GetFromCache<T>(cacheKey, fieldValue);
        }

        private async Task<T> GetFromCache<T>(string caheKey, string fieldValue)
        {
            if (string.IsNullOrEmpty(caheKey))
                return default;

            var item = await table.GetItemAsync(caheKey);

            if (item is null)
                return default;

            var ttl = item["ttl"].AsLong();
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();

            if (ttl < now)
                return default;

            var sabreSession = item[fieldValue].AsString();
            return JsonSerializer.Deserialize<T>(sabreSession);
        }

        public async Task<bool> InsertOrUpdate<T>(string cacheKey, T value, string fieldValue, string pccKeyValue = "")
        {
            try
            {
                Document result;
                var item = await table.GetItemAsync(cacheKey);

                if (item is null)
                {
                    item = new Document
                    {
                        ["cache_key"] = cacheKey,
                        ["ttl"] = DateTimeOffset.Now.AddMinutes(expiry).ToUnixTimeSeconds(),
                        [fieldValue] = JsonSerializer.Serialize(value)
                    };

                    if (!string.IsNullOrEmpty(pccKeyValue))
                        item["pcc_key"] = pccKeyValue;

                    result = await table.PutItemAsync(item, new PutItemOperationConfig() { ReturnValues = ReturnValues.AllNewAttributes });
                }
                else
                {
                    item["ttl"] = DateTimeOffset.Now.AddMinutes(expiry).ToUnixTimeSeconds();
                    item[fieldValue] = JsonSerializer.Serialize(value);

                    if (!string.IsNullOrEmpty(pccKeyValue))
                        item["pcc_key"] = pccKeyValue;

                    result = await table.UpdateItemAsync(item, new UpdateItemOperationConfig() { ReturnValues = ReturnValues.UpdatedNewAttributes });
                }

                return result is { };
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured saving sabre session to cache database. {SabreSession} {ErrorMessage} {Stacktrace}.", JsonSerializer.Serialize(value), ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> InsertSabreSession(string sessionid, string cacheKey)
        {
            //insert
            var newDoc = new Document();
            newDoc["expiry"] = DateTimeOffset.Now.AddMinutes(19).ToUnixTimeSeconds();
            newDoc["cache_key"] = cacheKey;
            newDoc["sabre_session_id"] = sessionid;

            var result = await table.PutItemAsync(newDoc);
            if (result is { })
            {
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteSabreSession(string cacheKey)
        {
            var doc = await table.GetItemAsync(cacheKey);
            if (doc != null)
            {
                var result = await table.DeleteItemAsync(doc);
                if (result is { })
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> InsertPNR(string cacheKey, PNR pnr, int expiryinmins)
        {
            //insert
            var newDoc = new Document();
            newDoc["expiry"] = DateTimeOffset.Now.AddMinutes(expiryinmins).ToUnixTimeSeconds();
            newDoc["cache_key"] = cacheKey;
            newDoc["pnr"] = JsonSerializer.Serialize(pnr);

            var result = await table.PutItemAsync(newDoc);
            if (result is { })
            {
                return true;
            }

            return false;
        }
    }
}