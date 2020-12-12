using SabreWebtopTicketingService.Models;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface INotificationHelper
    {
        Task NotifyTicketIssued(IssueTicketTransactionData data);
    }
}
