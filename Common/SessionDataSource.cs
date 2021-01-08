using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Text.Json;
using SabreWebtopTicketingService.Models;

namespace SabreWebtopTicketingService.Common
{
    public class SessionDataSource
    {
        private readonly ILogger _logger;
        private readonly string CACHE_DB = $"{Environment.GetEnvironmentVariable("ENVIRONMENT")??"stg"}-cache-data";

        private readonly AmazonDynamoDBClient dbClient;

        private readonly Table table;

        public SessionDataSource(ILogger logger)
        {
            dbClient = new AmazonDynamoDBClient();
            table = Table.LoadTable(dbClient, CACHE_DB);
            this._logger = logger;
        }


        public async Task<User> GetSessionUser(string sessionID)
        {
            var sessionUser = await GetFromDb<SessionUser>(sessionID);

            if(sessionUser != null && !string.IsNullOrEmpty(sessionUser.User?.ConsolidatorId) && sessionUser.User.ConsolidatorId == "internal")
            {
                sessionUser.User.ConsolidatorId = "acn";
            }
            return sessionUser?.User;
        }

        private async Task<T> GetFromDb<T>(string key)
        {
            _logger.LogInformation($"### RQ - GetFromDb('{key}') ###");
            var doc = await table.GetItemAsync(key);

            if (doc == null)
            {
                return default(T);
            }

            var rs = doc.ToJson();
            var ttl = doc["ttl"].AsLong();
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();

            if (ttl < now || string.IsNullOrWhiteSpace(rs))
            {
                return default(T);
            }

            _logger.LogInformation($"### RS - {rs}') ###");
            return JsonSerializer.Deserialize<T>(rs);
        }
    }
}
