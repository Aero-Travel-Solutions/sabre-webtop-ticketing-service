using Amazon.SQS;
using Amazon.SQS.Model;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;
using SabreWebtopTicketingService.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Common
{
    public class BackofficeDataSource : IBackofficeDataSource
    {
        private readonly ILogger _logger;
        private readonly ApiInvoker _apiInvoker;
        private readonly IAmazonSQS _sqs;
        private readonly string _sqsUrl;
        private readonly string _voidSqsUrl;

        public BackofficeDataSource(
                        ApiInvoker apiInvoker, 
                        IAmazonSQS amazonSQS,
                        ILogger logger)
        {
            _apiInvoker = apiInvoker;
            _sqs = amazonSQS;
            _logger = logger;
            _sqsUrl = Environment.GetEnvironmentVariable(Constants.BACKOFFICE_SQS_URL);
            _voidSqsUrl = Environment.GetEnvironmentVariable(Constants.BACKOFFICE_VOID_SQS_URL);
        }

        public async Task<AgencyCreditLimitResponse> GetAvailaCreditLimit(string customerNo, string sessionId)
        {
            try
            {
                var response = await _apiInvoker.InvokeApi(Constants.BACKOFFICE_URL,
                                HttpMethod.Get,
                                $"/getcreditlimit?customerno={customerNo}",
                                sessionId: sessionId);

                var result = await response.Content.ReadAsStringAsync();

                //TODO : Remove when its no longer needed
                _logger.LogInformation($"Customer no. {customerNo}. SessionId : {sessionId}. Response : {result}".Mask());
                return JsonSerializer.Deserialize<AgencyCreditLimitResponse>(result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                var errorMessage = $"BACKOFFICE_ERROR : An error occurred getting credit limit for customer {customerNo}.";
                _logger.LogError($"{errorMessage} {ex.Message}");

                throw new AeronologyException("BACKOFFICE_UNKNOWN_ERROR", errorMessage);
            }            
        }

        public async Task Book(IssueTicketTransactionData issueTicketTransactionData)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(issueTicketTransactionData);
                await SendToQueue(messageBody, _sqsUrl);
            }
            catch(Exception ex)
            {
                _logger.LogError("BACKOFFICE_BOOK_ERROR: {ex.Message}, {Exception}", ex.Message, ex);
            }            
        }

        public async Task VoidTickets(BackofficeVoidTicketRequest voidTicketRequest)
        {
            try
            {
                await SendToQueue(JsonSerializer.Serialize(voidTicketRequest), _voidSqsUrl);
            }
            catch(Exception ex)
            {
                _logger.LogError("BACKOFFICE_VOID_ERROR: {ex.Message}, {Exception}", ex.Message, ex);
            }
        }

        private async Task SendToQueue(string messageBody, string sqsQueueUrl)
        {
            var sendMessageResponse = await _sqs.SendMessageAsync(new SendMessageRequest()
            {
                QueueUrl = sqsQueueUrl,
                MessageBody = messageBody
            });

            if(sendMessageResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("BACKOFFICE_QUEUE_ERROR {StatusCode}, {ResponseMessage}, {MessageBodyRequest}", sendMessageResponse.HttpStatusCode, sendMessageResponse.MD5OfMessageBody, messageBody);
            }
        }
    }
}
