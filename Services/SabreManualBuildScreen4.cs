using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;

namespace SabreWebtopTicketingService.Services
{
    internal class SabreManualBuildScreen4
    {
        string mask = "";
        IssueExpressTicketQuote quote;
        public SabreManualBuildScreen4(string resp, IssueExpressTicketQuote quote)
        {
            mask = resp;
            this.quote = quote;

            if (!Success)
            {
                throw new GDSException("UNKNOWN_MASK", mask);
            }
        }

        internal bool Success => mask.IsMatch("TICKET  FARE INFO - DEPRESS ENTER WHEN COMPLETE");

        internal string[] Lines => mask.SplitOn("\n");

        public string Command
        {
            get
            {
                string returncommand = "WI";
                return returncommand;
            }
            internal set
            {
            }
        }
    }
}