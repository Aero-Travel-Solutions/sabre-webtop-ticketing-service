using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.CustomException
{
    public class AeronologyException : Exception, IDisposable
    {
        public readonly string ErrorMessage;
        public readonly string ErrorCode;

        public AeronologyException(string code, string message) : base(message)
        {
            ErrorCode = code;
            ErrorMessage = message;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
