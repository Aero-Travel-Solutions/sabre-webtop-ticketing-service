using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IOrdersTransactionDataSource
    {
        Task<string> GetOrderSequence();
    }
}
