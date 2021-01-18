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
        private readonly string CACHE_DB = $"{Environment.GetEnvironmentVariable("ENVIRONMENT")??"dev"}-cache-data";
        private readonly SessionRefreshService _sessionRefreshService;
        private readonly ILogger _logger;
        private readonly Table table;

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

        public async Task<T> Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            try
            {
                _logger.LogInformation($"### RQ - DB cache get('{key}') ###");

                var doc = await table.GetItemAsync(key);

                if (doc == null)
                {
                    _logger.LogInformation($"### DB cache get('{key}') => empty ###");
                    return default;
                }

                string jsonrs = doc.ToJson();

                _logger.LogInformation($"### RS - DB cache get('{key}') => {jsonrs} ###");


                return JsonSerializer.Deserialize<T>(jsonrs);

            }
            catch (Exception ex)
            {
                _logger.LogError($"### DB cache get('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
                return default;
            }
        }

        public async Task<bool> Set<T>(T item, string key, int expiryinmins)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                _logger.LogInformation($"### DB cache set('{key}') ###");

                var newDoc = new Document();
                string json = JsonSerializer.Serialize(item);
                newDoc[key] = json;

                var result = await table.PutItemAsync(newDoc);

                if (result is { })
                {
                    _logger.LogInformation($"### DB cache set('{key}') => empty ###");
                    return default;
                }

                return false;

            }
            catch (Exception ex)
            {
                _logger.LogError($"### DB cache get('{key}') ### => ERROR => {ex.Message} => ${ex.StackTrace}");
                return default;
            }
        }

        public async Task<SabreSession> GetSession(string key, Pcc pcc)
        {
            var doc = await table.GetItemAsync(key);

            if (doc == null)
            {
                return null;
            }

            var rs = doc.ToJson();
            var ttl = doc["expiry"].AsLong();
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();

            if (ttl < now || string.IsNullOrWhiteSpace(rs))
            {
                return null;
            }

            //if expires in less than 5 minutes, refresh session
            if (ttl > now && (ttl - now) < 300)
            {
                var refreshSuccess = await _sessionRefreshService.RefreshSessionToken(doc["sabre_session_id"], pcc);
                if (refreshSuccess)
                {
                    _logger.LogInformation("Session Refresh Successful => {0}", doc["sabre_session_id"]);
                }
                else
                {
                    _logger.LogError("Unable to Refresh Session => {0}", doc["sabre_session_id"]);
                }
            }

            SabreSession sabreSession =  JsonSerializer.Deserialize<SabreSession>(rs);
            sabreSession.Stored = true;
            return sabreSession;
        }

        public async Task<bool> InsertUpdateSabreSession(SabreSession sabreSession, string cacheKey)
        {
            var successInsert = false;
            if (sabreSession is { })
            {
                var doc = await table.GetItemAsync(cacheKey);
                if (doc is { })
                {
                    //update                    
                    doc["sabre_session_id"] = sabreSession.SessionID;
                    doc["expiry"] = DateTimeOffset.Now.AddMinutes(19).ToUnixTimeSeconds();
                    var result = await table.UpdateItemAsync(doc);
                    if (result is { })
                    {
                        successInsert = true;
                    }
                }
                else
                {
                    //insert
                    var newDoc = new Document();
                    newDoc["expiry"] = DateTimeOffset.Now.AddMinutes(19).ToUnixTimeSeconds();
                    newDoc["cache_key"] = cacheKey;
                    newDoc["sabre_session_id"] = sabreSession.SessionID;

                    var result = await table.PutItemAsync(newDoc);
                    if (result is { })
                    {
                        successInsert = true;
                    }
                }
            }   
            
            return successInsert;
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

        public async Task<bool> InsertStoreCC(string cacheKey, List<StoredCreditCard> ccs, int expiryinmins)
        {
            //insert
            var newDoc = new Document();
            newDoc["expiry"] = DateTimeOffset.Now.AddMinutes(expiryinmins).ToUnixTimeSeconds();
            newDoc["cache_key"] = cacheKey;
            newDoc["pnr"] = JsonSerializer.Serialize(ccs);

            var result = await table.PutItemAsync(newDoc);
            if (result is { })
            {
                return true;
            }

            return false;
        }
    }
}