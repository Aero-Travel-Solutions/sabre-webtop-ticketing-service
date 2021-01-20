using SabreWebtopTicketingService.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SabreWebtopTicketingService.Interface
{
    public interface IDbCache
    {
        Task<List<SabreSession>> ListSabreSessions(string pccKey, string fieldValue);
        Task<int> SabreSessionCount(string pccKey);
        Task<T> Get<T>(string cacheKey, string fieldValue);
        Task<bool> InsertOrUpdate<T>(string cacheKey, T value, string fieldValue, string pccKeyValue = "");
        Task<bool> DeleteSabreSession(string cacheKey);
    }
}
