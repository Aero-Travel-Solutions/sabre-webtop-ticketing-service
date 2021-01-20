using SabreWebtopTicketingService.Models;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface ICommissionDataService
    {
        string ContextID { get; set; }
        Task<CalculateCommissionResponse> Calculate(CalculateCommissionRequest request);
    }
}
