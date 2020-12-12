using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class BCodeDataSource : IBCodeDataSource
    {
        private readonly ILogger logger;
        private readonly AmazonLambdaClient client;
        private readonly string env;
        public string ContextID { get; set; }

        public BCodeDataSource(
            AmazonLambdaClient _client, 
            ILogger _logger)
        {
            client = _client;
            logger = _logger;
            env = System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev";
        }

        public async Task<string> RetrieveBCode(string sessionid, string airline, string dKNumber)
        {
            BCodeRequest rq = new BCodeRequest()
            {
                SessionId = sessionid,
                Airline = airline,
                CustomerNo = dKNumber
            };
            var response = await InvokeLambdaCShap<string>(
                                    $"bcode-database-{env}-RetrieveValidBCodes",
                                    rq);

            return response;
        }

        public async Task<string> InvokeLambdaCShap<R>(string functionName, object input)
        {
            var lambdaPayload = string.Empty;
            try
            {
                var invokeRequest = new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = JsonConvert.
                                    SerializeObject(
                                        input,
                                        new JsonSerializerSettings()
                                        {
                                            ContractResolver = new DefaultContractResolver()
                                            {
                                                NamingStrategy = new SnakeCaseNamingStrategy()
                                                {
                                                    OverrideSpecifiedNames = false
                                                }
                                            }
                                        })
                };

                logger.LogInformation($"***** Invoking lambda {functionName} RQ => {invokeRequest.Payload} *****".Mask());
                var lambdaResponse = await client.InvokeAsync(invokeRequest);

                using (var sr = new StreamReader(lambdaResponse.Payload))
                {
                    lambdaPayload = await sr.ReadToEndAsync();
                    if(lambdaPayload.Contains("context_id"))
                    {
                        var errorpayload = JsonConvert.
                                                DeserializeObject<BCodeErrorResponse>(lambdaPayload);

                        logger.LogInformation($"***** ERROR lambda {functionName} RS => {lambdaPayload} *****".Mask());
                        throw new AeronologyException("BCode_ERROR", $"{errorpayload.code} - {errorpayload.message}.");
                    }
                    var payload = JsonConvert.
                                            DeserializeObject<string>(lambdaPayload);
                    logger.LogInformation($"***** Invoking lambda {functionName} RS => {lambdaPayload} *****".Mask());
                    return payload;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while processing lambda request {input} lambda method {functionName}. Lambda response {lambdaPayload} and {ex.Message}.");
                throw;
            }
        }
    }

    public class BCodeRequest
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }
        [JsonPropertyName("airline")]
        public string Airline { get; set; }
        [JsonPropertyName("customer_no")]
        public string CustomerNo { get; set; }
    }

    public class BCodeErrorResponse
    {
        public string context_id { get; set; }
        public string code { get; set; }
        public string message { get; set; }
    }
}
