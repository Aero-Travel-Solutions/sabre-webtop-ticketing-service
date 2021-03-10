using SabreWebtopTicketingService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IQueueManagementDataSource
    {
        Task<QueueModel> RetrieveQueueRecord(string sessionId, string queueID);
    }
}
