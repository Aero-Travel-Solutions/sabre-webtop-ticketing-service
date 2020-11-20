using Microsoft.Extensions.Caching.Distributed;
using Polly;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;

namespace SabreWebtopTicketingService.PollyPolicies
{
    public class ExpiredTokenRetryPolicy
    {
        public readonly IAsyncPolicy ExpiredTokenPolicy;
        
        private readonly IDistributedCache _cacheManager;
        private readonly ILogger _logger;

        public ExpiredTokenRetryPolicy(ILogger logger, IDistributedCache cacheManager)
        {
            _logger = logger;           
            _cacheManager = cacheManager;

            ExpiredTokenPolicy = Policy
                .Handle<ExpiredTokenException>()                
                .RetryAsync(1, (ex, count) =>
                {
                    _logger.LogError($"[RETRY] Error : {ex.Message} Retry Count : {count}");
                    _cacheManager.RemoveAsync(((ExpiredTokenException)ex).SessionCacheKey);
                });
        }
    }
}
