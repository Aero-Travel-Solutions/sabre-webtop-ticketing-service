using System;

namespace SabreWebtopTicketingService.CustomException
{
    public class ExpiredSessionException : Exception
    {
        public readonly string ErrorMessage;
        public readonly string ErrorCode;
        public readonly string SessionID;

        public ExpiredSessionException(string sessionId, string code, string message) : base(message)
        {
            SessionID = sessionId;
            ErrorCode = code;
            ErrorMessage = message;
        }
    }
}
