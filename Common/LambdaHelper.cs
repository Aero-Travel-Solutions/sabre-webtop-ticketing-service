using System;
using System.IO;
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

                var lambdaResponse = await client.InvokeAsync(new InvokeRequest
                {
                    FunctionName = functionName,
                        InvocationType = InvocationType.RequestResponse,
                        Payload = JsonSerializer.Serialize(
                            new { session_id = sessionid, data = input }, 
                            new JsonSerializerOptions() 
                            { 
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                            })
                });

                using(var sr = new StreamReader(lambdaResponse.Payload))
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
    }

    public class Error
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("stack")]
        public string Stack { get; set; }
    }
}
