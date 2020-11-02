using SabreWebtopTicketingService.Models;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IGetTurnaroundPointDataSource
    {
        Task<string> GetTurnaroundPoint(GetTurnaroundPointRequest getTurnaroundPointRequest);
    }
}
