using SabreWebtopTicketingService.Models;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IBackofficeDataSource
    {
        Task<AgencyCreditLimitResponse> GetAvailaCreditLimit(string customerNo, string sessionId);
        Task Book(IssueTicketTransactionData issueTicketTransactionData);
        Task VoidTickets(BackofficeVoidTicketRequest voidTicketRequest);
    }
}
