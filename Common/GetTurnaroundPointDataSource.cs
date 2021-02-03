using Amazon.Lambda;
using Amazon.Lambda.Model;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class GetTurnaroundPointDataSource : IGetTurnaroundPointDataSource
    {
        private readonly ILogger logger;
        private readonly AmazonLambdaClient client;
        private readonly string env;
        public string ContextID { get; set; }


        public GetTurnaroundPointDataSource(
            ILogger logger,
            AmazonLambdaClient client)
        {
            this.logger = logger;
            this.client = client;
            env = System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev";
        }
        public async Task<string> GetTurnaroundPoint(GetTurnaroundPointRequest getTurnaroundPointRequest)
        {
            var response = await InvokeLambdaCShap<string>(
                                    $"aero-calc-{env}-getTurnAroundPoint",
                                    getTurnaroundPointRequest);

            return response.LastMatch(@"[a-zA-Z]{3}");
        }

        public async Task<string> InvokeLambdaCShap<R>(string functionName, object input)
        {
            var lambdaPayload = "";
            string inputjson = "";
            try
            {
                inputjson = JsonSerializer.Serialize(input, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var invokeRequest = new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = inputjson
                };

                logger.LogInformation($"Invoking lambda function {functionName} with {inputjson}");
                var lambdaResponse = await client.InvokeAsync(invokeRequest);

                using (var sr = new StreamReader(lambdaResponse.Payload))
                {
                    lambdaPayload = await sr.ReadToEndAsync();

                    logger.LogInformation($"Lambda function {functionName} response {lambdaPayload}");
                    return lambdaPayload;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while processing lambda request {inputjson} lambda method {functionName}. Lambda response {lambdaPayload} and {ex.Message}.");
                throw;
            }
        }
        public class LambdaPayload<T>
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
