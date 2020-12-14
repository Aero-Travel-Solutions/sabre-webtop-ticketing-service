using Amazon.Lambda;
using Amazon.Lambda.Model;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class MerchantDataSource : IMerchantDataSource
    {
        private readonly ILogger logger;
        private readonly AmazonLambdaClient client;
        private readonly string env;
        public string ContextID { get; set; }

        public MerchantDataSource(
            AmazonLambdaClient _client,
            ILogger _logger)
        {
            client = _client;
            logger = _logger;
            env = System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "dev";
        }

        public async Task<MerchantLambdaResponse> CapturePayment(string sessionId, string consolidatorid, string paymentsessionId, string orderid, decimal amount, string desc = "")
        {
            CapturePaymentRequest rq = new CapturePaymentRequest()
            {
                session_id = sessionId,
                payment_session_id = paymentsessionId,
                order_id = orderid,
                amount = Math.Round(amount, 2),
                description = desc
            };

            var response = await InvokeLambdaCShap<string>(
                                    $"{consolidatorid}-merchant-payment-{env}-capture",
                                    rq);

            return response;
        }

        public async Task<MerchantLambdaResponse> CancelHold(string sessionId, string consolidatorid, string paymentsessionId, string orderid)
        {
            CancelHoldRequest rq = new CancelHoldRequest()
            {
                session_id = sessionId,
                payment_session_id = paymentsessionId,
                order_id = orderid
            };

            var response = await InvokeLambdaCShap<string>(
                        $"{consolidatorid}-merchant-payment-{env}-void",
                        rq);

            return new MerchantLambdaResponse()
            {
                Success = response.Success
            };
        }

        public async Task<MerchantLambdaResponse> InvokeLambdaCShap<R>(string functionName, object input)
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
                        return new MerchantLambdaResponse()
                        {
                            Success = false
                        };
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
    }

    public class CapturePaymentRequest
    {
        public string session_id { get; set; }
        public string payment_session_id { get; set; }
        public string order_reference { get; set; }
        public string order_id { get; set; }
        public string description { get; set; }
        public decimal amount { get; set; }
    }

    public class CancelHoldRequest
    {
        public string session_id { get; set; }
        public string payment_session_id { get; set; }
        public string order_id { get; set; }
    }


    public class Data
    {
        public string code { get; set; }
        public string recommendation { get; set; }
        public string result { get; set; }
        public string approval_code { get; set; }
        public string card_number { get; set; }
        public string card_type { get; set; }
        public string card_expiry { get; set; }
    }

    public class MerchantError
    {
        public string code { get; set; }
        public string message { get; set; }
        public object stack { get; set; }
    }

    public class Body
    {
        public string session_id { get; set; }
        public string context_id { get; set; }
        public Data data { get; set; }
        public Error error { get; set; }
    }

    public class MerchantLambdaPayload
    {
        public int statusCode { get; set; }
        //public Headers headers { get; set; }
        public string body { get; set; }
        public bool isBase64Encoded { get; set; }
    }
}
