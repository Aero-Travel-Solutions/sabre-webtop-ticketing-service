using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.EntityFrameworkCore.Internal;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class OrdersTransactionDataSource : IOrdersTransactionDataSource
    {
        private readonly AmazonDynamoDBClient _amazonDynamoDBClient;
        private readonly string _tableName;
        public OrdersTransactionDataSource()
        {
            _amazonDynamoDBClient = new AmazonDynamoDBClient();
            _tableName = $"{Environment.GetEnvironmentVariable("ENVIRONMENT")}-transactiondb";
        }

        public async Task<string> GetOrderSequence()
        {
            int  orderSequence;
            var hkey = $"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month:00}{DateTime.UtcNow.Day:00}";

            try
            {                
                //query todays order
                var queryRequest = new QueryRequest()
                {
                    TableName = _tableName,
                    KeyConditionExpression = "hash_key = :hkey",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":hkey", new AttributeValue{ S = hkey } }
                },
                    Limit = 1,
                    ScanIndexForward = false,
                    ConsistentRead = true
                };

                var queryResponse = await _amazonDynamoDBClient.QueryAsync(queryRequest);

                if (queryResponse.HttpStatusCode != HttpStatusCode.OK)
                    throw new GetOrderSequenceException("A system network issue encountered.");

                if (queryResponse?.Items?.Any() ?? false)
                {
                    //update record
                    queryResponse.Items.FirstOrDefault().TryGetValue("order_seq", out AttributeValue sortValue);
                    var currentSeq = int.Parse(sortValue.S);
                    orderSequence = currentSeq + 1;

                    var updateRequest = new UpdateItemRequest()
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>() 
                        { 
                            { "hash_key", new AttributeValue { S = hkey } },
                            { "sort_key", new AttributeValue {S = hkey} }
                        },
                        ExpressionAttributeNames = new Dictionary<string, string>() { { "#seq", "order_seq"} },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                        { 
                            { ":nVal", new AttributeValue { S = orderSequence.ToString() } },
                            { ":cVal", new AttributeValue { S = currentSeq.ToString()} }
                        },
                        UpdateExpression = "SET #seq = :nVal",
                        ConditionExpression = $"#seq = :cVal"
                    };

                    var updateResult = await _amazonDynamoDBClient.UpdateItemAsync(updateRequest);
                }
                else
                {
                    orderSequence = 1;
                    //Insert
                    var insertItemRequest = new PutItemRequest()
                    {
                        TableName = _tableName,
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            {"hash_key", new AttributeValue{ S = hkey } },
                            {"sort_key", new AttributeValue{ S = hkey } },
                            {"order_seq", new AttributeValue{ S = orderSequence.ToString()} }
                        },
                        ConditionExpression = "attribute_not_exists(hash_key)"
                    };

                    var result = await _amazonDynamoDBClient.PutItemAsync(insertItemRequest);
                }
            }
            catch (ConditionalCheckFailedException ce)
            {
                throw new GetOrderSequenceException(ce.Message);
            }            

            return $"{hkey}{orderSequence:00}";
        }
    }
}
