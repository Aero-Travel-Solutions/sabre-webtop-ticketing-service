using Microsoft.Extensions.Logging;
using Polly;
using SabreWebtopTicketingService.CustomException;

namespace SabreWebtopTicketingService.PollyPolicies
{
    public  class GetOrderSquenceRetryPolicy
    {
        public readonly IAsyncPolicy CheckConditionFailedPolicy;
        private readonly ILogger<GetOrderSquenceRetryPolicy> _logger;

        public GetOrderSquenceRetryPolicy(ILogger<GetOrderSquenceRetryPolicy> logger)
        {
            _logger = logger;

            CheckConditionFailedPolicy = Policy
                .Handle<GetOrderSequenceException>()
                .RetryAsync(1, (ex, count) =>
                {
                    _logger.LogError($"GetOrderSequenceException: [RETRY] Error : {ex.Message} Retry Count : {count}");                 
                });
        }
    }
}
