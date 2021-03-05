using SabreWebtopTicketingService.Models;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IQueueManagementDataSource
    {
        Task<MerchantLambdaResponse> RetriveCardData(string sessionId, string consolidatorid, string paymentsessionId, string orderid, decimal amount, string desc);
    }
}
