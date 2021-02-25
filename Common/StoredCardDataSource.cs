﻿using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public interface IStoredCardDataSource
    {
        Task<List<StoredCreditCard>> Get(string key);
        Task Save(string key, List<StoredCreditCard> storedCreditCards);
    }
    public class StoredCardDataSource : IStoredCardDataSource
    {
        private readonly IKMSHelper _kMSHelper;        
        private readonly AmazonDynamoDBClient dbClient;
        private readonly string CACHE_DB = $"{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev"}-cache-data";
        private readonly Table table;
        private readonly ILogger _logger;

        public StoredCardDataSource(
            IKMSHelper kMSHelper,
            ILogger logger)
        {
           
            dbClient = new AmazonDynamoDBClient();
            table = Table.LoadTable(dbClient, CACHE_DB);
            _kMSHelper = kMSHelper;
            _logger = logger;
        }

        public async Task<List<StoredCreditCard>> Get(string key)
        {
            try
            {
                _logger.LogMaskInformation("Inside StoredCardDataSource => Get");
                _logger.LogMaskInformation($"Inside StoredCardDataSource => {key}");

                var storedCardsFromCache = await GetAsync(key, "cc_info");


                if (string.IsNullOrEmpty(storedCardsFromCache))
                {
                    _logger.LogMaskInformation("Key not found in database");
                    return default;
                }

                var storedCards = JsonSerializer.Deserialize<List<StoredCreditCard>>(storedCardsFromCache);

                //Decrypt
                foreach(var card in storedCards)
                {
                    card.CreditCard = await _kMSHelper.Decrypt(card.CreditCard);
                }

                return storedCards;
            }
            catch(Exception ex)
            {
                _logger.LogError($"StoredCardDataSource => Get Error: {ex}");
                return default;
            }
        }

        public async Task Save(string key, List<StoredCreditCard> storedCardNumbers)
        {
            try
            {
                foreach (var card in storedCardNumbers)
                {
                    card.CreditCard = await _kMSHelper.Encrypt(card.CreditCard);
                }

                await InsertOrUpdateAsync(key, "cc_info", JsonSerializer.Serialize(storedCardNumbers), 15);
            }
            catch(Exception ex)
            {
                _logger.LogError($"StoredCardDataSource => Save Error: {ex}");
                throw;
            }
        }

        private async Task InsertOrUpdateAsync(string key, string attributeName, string val, double expirationInMins = 30)
        {
            var item = await table.GetItemAsync(key);

            if (item is null)
            {
                _logger.LogMaskInformation("New item insert.");

                item = new Document
                {
                    ["cache_key"] = key,
                    ["ttl"] = DateTimeOffset.Now.AddMinutes(expirationInMins).ToUnixTimeSeconds(),
                    [attributeName] = JsonSerializer.Serialize(val)
                };

                await table.PutItemAsync(item);
            }
            else
            {
                _logger.LogMaskInformation("Item found on DB.");
                item["ttl"] = DateTimeOffset.Now.AddMinutes(expirationInMins).ToUnixTimeSeconds();
                item[attributeName] = JsonSerializer.Serialize(val);

                await table.UpdateItemAsync(item);
            }
        }

        private async Task<string> GetAsync(string key, string attributeName)
        {
            var doc = await table.GetItemAsync(key);
            if (doc == null)
            {
                _logger.LogError("GetAsync => No item found on DB");
                return default;
            }
            return doc[attributeName];
        }
    }
}
