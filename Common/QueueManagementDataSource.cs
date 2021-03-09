using Amazon.Lambda;
using Amazon.Lambda.Model;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class QueueManagementDataSource : IQueueManagementDataSource
    {
        private readonly ILogger logger;
        private readonly AmazonLambdaClient client;
        private readonly string env;
        public string ContextID { get; set; }

        public QueueManagementDataSource(
            AmazonLambdaClient _client,
            ILogger _logger)
        {
            client = _client;
            logger = _logger;
            env = System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev";
        }

        public async Task<List<PNRStoredCards>> InvokeLambdaCShap<R>(string functionName, object input)
        {
            var lambdaPayload = string.Empty;
            string jsoninput = JsonSerializer.Serialize(input, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            try
            {
                var invokeRequest = new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = jsoninput
                };

                logger.LogMaskInformation($"Invoking lambda function {functionName} with {invokeRequest.Payload}");
                var lambdaResponse = await client.InvokeAsync(invokeRequest);

                using (var sr = new StreamReader(lambdaResponse.Payload))
                {
                    lambdaPayload = await sr.ReadToEndAsync();
                    var merchantpayload = JsonSerializer.Deserialize<MerchantLambdaPayload>(lambdaPayload);
                    var payload = JsonSerializer.Deserialize<Body>(merchantpayload.body);
                    ContextID = payload.context_id;
                    logger.LogMaskInformation($"Lambda function {functionName} response {lambdaPayload}");
                    
                    if(merchantpayload.statusCode != 200)
                    {
                        if (payload.error != null)
                        {
                            logger.LogError($"(Context ID - {payload.context_id}) {payload.error.Message}.");
                        }
                        return new List<PNRStoredCards>();
                    }

                    return new MerchantLambdaResponse()
                    {
                        Success = payload.data.result == "SUCCESS",
                        ApprovalCode = payload.data.result == "SUCCESS" ? payload.data.approval_code : "",
                        CardNumber = payload.data.result == "SUCCESS" ? payload.data.card_number : "",
                        CardType = payload.data.result == "SUCCESS" ? payload.data.card_type : "",
                        CardExpiry = payload.data.result == "SUCCESS" ? payload.data.card_expiry: ""
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Lambda request - {jsoninput.MaskLog()}. Lambda method - {functionName}. Lambda response {lambdaPayload} and {ex.Message}.");
                throw;
            }
        }

        public async Task<List<PNRStoredCards>> RetrieveCardData(string sessionId, string queueID, User user)
        {
            GetQueueModel rq = new GetQueueModel()
            {
                SessionId = sessionId,
                GDSSource = "1W",
                QueueID = queueID
            };

            var response = await InvokeLambdaCShap<string>(
                                    $"queue-pnr-control-{env}-retrieveDetailQueueRecord",
                                    rq);

            return response;
        }
    }

    public class GetQueueModel
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("hash_key")]
        public string HashKey { get; set; }

        [JsonPropertyName("sort_key")]
        public string SortKey { get; set; }

        [JsonPropertyName("queue_id")]
        public string QueueID { get; set; }

        [JsonPropertyName("plating_carrier")]
        public string PlatingCarrier { get; set; }

        [JsonPropertyName("gds_source")]
        public string GDSSource { get; set; }

        [JsonPropertyName("record_locator")]
        public string RecordLocator { get; set; }

        [JsonPropertyName("queue_date_from")]
        public string QueueDateFrom { get; set; }

        [JsonPropertyName("queue_date_to")]
        public string QueueDateTo { get; set; }

        [JsonPropertyName("departure_date_from")]
        public string DepartureDateFrom { get; set; }

        [JsonPropertyName("departure_date_to")]
        public string DepartureDateTo { get; set; }

        [JsonPropertyName("queue_statuses")]
        public List<string> QueueStatuses { get; set; }

        [JsonPropertyName("agency_name")]
        public string AgencyName { get; set; }

        [JsonPropertyName("flight_type")]
        public string FlightType { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonPropertyName("queue_type")]
        public string QueueType { get; set; }

        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }

        [JsonPropertyName("is_my_worklist")]
        public bool IsMyWorklist { get; set; }

        [JsonPropertyName("is_my_queues")]
        public bool IsMyQueues { get; set; }
        public string warmer { get; set; }
    }
}
