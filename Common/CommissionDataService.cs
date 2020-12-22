using Amazon.Lambda;
using Amazon.Lambda.Model;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class CommissionDataService : ICommissionDataService
    {
        private readonly ILogger logger;
        private readonly AmazonLambdaClient client;
        private readonly string env;
        public string ContextID { get; set; }

        public CommissionDataService(
                ILogger logger,
                AmazonLambdaClient client,
                SessionDataSource sessionDataSource)
        {
            this.logger = logger;
            this.client = client;
            env = System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev";
        }
        public async Task<CalculateCommissionResponse> Calculate(CalculateCommissionRequest request)
        {
            var response = await InvokeLambdaNodeJS<CalculateCommissionResponse>(
                $"commission-database-{env}-calculate",
                request);

            return response;
        }

        public async Task<R> InvokeLambdaNodeJS<R>(string functionName, object input)
        {
            var lambdaPayload = "";
            string jsoninput = "";

            try
            {
                var lambdaRequest = new CommissionLambdaPayload<object> { Body = JsonSerializer.Serialize(input) };
                jsoninput = JsonSerializer.Serialize(lambdaRequest, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                var invokeRequest = new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = jsoninput
                };

                logger.LogInformation($"Invoking lambda function {functionName} with {jsoninput}");
                var lambdaResponse = await client.InvokeAsync(invokeRequest);

                using (var sr = new StreamReader(lambdaResponse.Payload))
                {
                    lambdaPayload = await sr.ReadToEndAsync();
                    var payload = JsonSerializer.Deserialize<CommissionLambdaPayload<object>>(lambdaPayload);
                    ContextID = payload.ContextID;

                    logger.LogInformation($"Lambda function {functionName} response {lambdaPayload}");

                    if (payload.StatusCode != 200)
                    {
                        throw new CustomException.AeronologyException("50000078", $"(CommissionTraceID - {ContextID}){System.Environment.NewLine}{payload.Body}.");
                    }

                    return JsonSerializer.Deserialize<CommissionLambdaPayload<R>>(payload.Body).Result;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while processing lambda request {jsoninput.MaskLog()} lambda method {functionName}. Lambda response {lambdaPayload} and {ex.Message}.");
                throw;
            }
        }

        public class CommissionLambdaPayload<T>
        {
            [JsonPropertyName("statusCode")]
            public int StatusCode { get; set; }

            [JsonPropertyName("body")]
            public string Body { get; set; }

            [JsonPropertyName("app_version")]
            public string ApplicationVersion { get; set; }
            [JsonPropertyName("context_id")]
            public string ContextID { get; set; }
            [JsonPropertyName("result")]
            public T Result { get; set; }
        }
    }
}
