using System;

namespace SabreWebtopTicketingService.CustomException
{
    public class ExpiredTokenException : Exception
    {
        public readonly string SessionCacheKey;
        public readonly string ErrorMessage;
        public readonly string ErrorCode;

        public ExpiredTokenException(string sessionCacheKey, string code, string message) : base(message)
        {
            ErrorCode = code;
            ErrorMessage = message;
            SessionCacheKey = sessionCacheKey;
        }        
    }
}
