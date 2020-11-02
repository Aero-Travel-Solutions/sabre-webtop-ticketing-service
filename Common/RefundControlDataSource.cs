using Amazon.Lambda;
using Amazon.Lambda.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class RefundControlDataSource : IRefundControlDataSource
    {
        private readonly ILogger<RefundControlDataSource> logger;
        private readonly AmazonLambdaClient client;
        private readonly string env;
        public string ContextID { get; set; }

        public RefundControlDataSource(
            AmazonLambdaClient _client,
            ILogger<RefundControlDataSource> _logger)
        {
            client = _client;
            logger = _logger;
            env = System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev";
        }

        public async Task<RefundControlResponse> GetRefundControlRec(RefundControlRequest request)
        {

            var response = await InvokeLambdaCShap<string>(
                                    $"refund-control-database-{env}-retrieveValidFees",
                                    request);

            return response;
        }

        public async Task<RefundControlResponse> InvokeLambdaCShap<R>(string functionName, object input)
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

                logger.LogInformation($"Invoking lambda function {functionName} with {invokeRequest.Payload}");
                var lambdaResponse = await client.InvokeAsync(invokeRequest);

                using (var sr = new StreamReader(lambdaResponse.Payload))
                {
                    lambdaPayload = await sr.ReadToEndAsync();
                    if (lambdaPayload.Contains("context_id"))
                    {
                        var errorpayload = JsonConvert.
                                                DeserializeObject<RefundControlErrorResponse>(lambdaPayload);

                        throw new CustomException.AeronologyException("BCode_ERROR", $"{errorpayload.code} - {errorpayload.message}.");
                    }
                    var payload = JsonConvert.
                                            DeserializeObject<RefundControlResponse>(lambdaPayload);
                    logger.LogInformation("Lambda function {FunctionName} response {Response}", functionName, lambdaPayload);
                    return payload;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing lambda request {Request} lambda method {FunctionName}. Lambda response {LambdaPayload} and {ErrorMessage}.", input, functionName, lambdaPayload, ex.Message);
                throw;
            }
        }
    }


    public class RefundControlErrorResponse
    {
        public string context_id { get; set; }
        public string code { get; set; }
        public string message { get; set; }
    }

}
