using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace SabreWebtopTicketingService.Common
{
    public class LambdaHelper
    {
        private readonly ILogger logger;

        private readonly AmazonLambdaClient client;

        private readonly SessionDataSource session;

        public LambdaHelper(ILogger logger, AmazonLambdaClient client, SessionDataSource session)
        {
            this.logger = logger;
            this.client = client;
            this.session = session;
        }

        public async Task<T> Invoke<T>(string functionName, object input, string sessionid)
        {
            try
            {
                logger.LogInformation($"***** Invoking lambda {functionName} *****");

                string inputjson = JsonSerializer.Serialize(
                            new { session_id = sessionid, data = input },
                            new JsonSerializerOptions()
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                var lambdaResponse = await client.InvokeAsync(new InvokeRequest
                {
                    FunctionName = functionName,
                        InvocationType = InvocationType.RequestResponse,
                        Payload = inputjson
                });

                logger.LogInformation($"Invoking lambda function {functionName} with {inputjson}");

                using (var sr = new StreamReader(lambdaResponse.Payload))
                {
                    var data = await sr.ReadToEndAsync();
                    logger.LogInformation($"***** RS: {functionName} => {data} ******");
                    var response = JsonSerializer.Deserialize<LambdaPayload<T>>(data);

                    if (response.Error != null)
                    {
                        logger.LogError($"{response.Error.Message} --- Service stack: {response.Error.Stack}");
                        throw new Exception($"{response.Error.Message} --- Context Id: {response.ContextId} --- Session Id: {response.SessionId}");
                    }

                    return response.Data;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw;
            }
        }
    }

    public class LambdaPayload<T>
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("context_id")]
        public string ContextId { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("error")]
        public Error Error { get; set; }
        public int StatusCode { get; internal set; }
    }

    public class Error
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("stack")]
        public string Stack { get; set; }
    }

    public class DataAgent
    {
        public string agent_id { get; set; }
        public List<string> opt_in { get; set; }
        public string gds_code { get; set; }
        public DateTime modified_date { get; set; }
        public string consolidator_id { get; set; }
        public string created_by { get; set; }
        public DateTime created_date { get; set; }
        public string sort_key { get; set; }
        public bool is_default { get; set; }
        public string modified_by { get; set; }
        public string name { get; set; }
        public string data_type { get; set; }
        public string hash_key { get; set; }
        public string pcc_code { get; set; }
        public string description { get; set; }
        public string country_code { get; set; }
    }
}
