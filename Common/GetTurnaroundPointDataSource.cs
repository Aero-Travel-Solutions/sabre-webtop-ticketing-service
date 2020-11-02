using Amazon.Lambda;
using Amazon.Lambda.Model;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GetTurnaroundPointDataSource> logger;
        private readonly AmazonLambdaClient client;
        private readonly string env;
        public string ContextID { get; set; }


        public GetTurnaroundPointDataSource(
            ILogger<GetTurnaroundPointDataSource> logger,
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
            var lambdaPayload = string.Empty;
            try
            {
                var invokeRequest = new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = JsonSerializer.Serialize(input, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                };

                logger.LogInformation("Invoking lambda function {FunctionName} with {Request}", functionName, invokeRequest);
                var lambdaResponse = await client.InvokeAsync(invokeRequest);

                using (var sr = new StreamReader(lambdaResponse.Payload))
                {
                    lambdaPayload = await sr.ReadToEndAsync();

                    logger.LogInformation("Lambda function {FunctionName} response {Response}", functionName, lambdaPayload);
                    return lambdaPayload;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing lambda request {Request} lambda method {FunctionName}. Lambda response {LambdaPayload} and {ErrorMessage}.", input, functionName, lambdaPayload, ex.Message);
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
