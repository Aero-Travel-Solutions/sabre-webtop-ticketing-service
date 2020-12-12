using SabreWebtopTicketingService.Models;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IMerchantDataSource
    {
        Task<MerchantLambdaResponse> CapturePayment(string sessionId, string consolidatorid, string paymentsessionId, string orderid, decimal amount, string desc);
        Task<MerchantLambdaResponse> CancelHold(string sessionId, string consolidatorid, string paymentsessionId, string orderid);
    }
}
