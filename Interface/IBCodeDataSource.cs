using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IBCodeDataSource
    {
        Task<string> RetrieveBCode(string sessionid, string airline, string dKNumber);
    }
}
