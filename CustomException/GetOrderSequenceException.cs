using System;

namespace SabreWebtopTicketingService.CustomException
{
    public class GetOrderSequenceException : Exception
    {
        public GetOrderSequenceException(string message) : base(message) { }
    }
}
