
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface ICacheDataSource
    {
        Task<T> Get<T>(string key);        

        Task Set<T>(string key, T value);

        Task Delete(string key);

        Task ListRightPushAsync<T>(string key, T value);
        Task<long> ListLengthAsync(string key);
        Task<List<T>> ListRangeAsync<T>(string key);
        Task ListRemoveAtAsync(string key, int index);
    }
}
