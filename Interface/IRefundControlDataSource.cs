using SabreWebtopTicketingService.Models;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IRefundControlDataSource
    {
        Task<RefundControlResponse> GetRefundControlRec(RefundControlRequest request);
    }
}
