using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using System.Text.Json;
using SabreWebtopTicketingService.Interface;
using SabreWebtopTicketingService.Models;

namespace SabreWebtopTicketingService.Common
{
    public class NotificationHelper : INotificationHelper
    {
        private readonly string region;

        private readonly string ticketIssuedTopic;

        private readonly ILogger logger;

        public NotificationHelper(ILogger logger)
        {
            region = Environment.GetEnvironmentVariable("REGION") ?? "ap-southeast-2";
            ticketIssuedTopic = Environment.GetEnvironmentVariable("SNS_TOPIC_TICKETS_ISSUED");
            this.logger = logger;
        }

        public async Task NotifyTicketIssued(IssueTicketTransactionData data)
        {
            logger.LogInformation($"Tickets issued for {data.TicketingResult.Locator}");

            try
            {
                using (var client = new AmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var message = JsonSerializer.Serialize(data);
                    await client.PublishAsync(ticketIssuedTopic, message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }
    }
}
