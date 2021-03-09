using SabreWebtopTicketingService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IQueueManagementDataSource
    {
        Task<List<PNRStoredCards>> RetrieveCardData(string sessionId, string consolidatorid, string paymentsessionId, string orderid, decimal amount, string desc);
    }
}
