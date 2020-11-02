using System;

namespace SabreWebtopTicketingService.CustomException
{
    public class GDSException : Exception, IDisposable
    {
        public readonly string ErrorMessage;
        public readonly string ErrorCode;
        public GDSException(string code, string message) : base(message)
        {
            ErrorMessage = message;
            ErrorCode = code;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
