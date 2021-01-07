using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System.Linq;

namespace SabreWebtopTicketingService.Services
{
    internal class SabreManualBuildScreen4
    {
        //WI TICKET FARE AMOUNT<2 >
        // TA-PSGR- ADT
        // FILL IN LEG FARES/SURCHARGES/MILEAGE/STOPOVER CHARGES/ETC
        // DEPRESS ENTER WHEN COMPLETE THEN RESET AND CLEAR TO RETURN
        // TO PNR

        // F/CALC<MEL QF HKG337.91/-SIN QF MEL315.63NUC653.54END ROE1.3>
        //<46491                                                       >
        //<                                                            >
        //<                                                            >
        //<                                                            >


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

        internal bool Success => mask.IsMatch("WI TICKET FARE AMOUNT");

        internal string[] Lines => mask.SplitOn("\n");

        public string Command
        {
            get
            {
                string returncommand = "WI";
                //TKT RECORD RECORD
                if (Lines.Any(a => a.Contains(@"TICKET FARE AMOUNT<")))
                {
                    string value = Lines.First(f => f.IsMatch(@"TICKET\sFARE\sAMOUNT\s*<")).LastMatch(@"TICKET\sFARE\sAMOUNT\s*<([\s\d]*)>");
                    returncommand += string.IsNullOrWhiteSpace(value) ? "<>" : $"<{value.Trim()}>";
                }

                returncommand += mask.SplitOn("F/CALC").Last().Trim();

                return returncommand.Replace("\n", "");
            }
            internal set
            {
            }
        }
    }
}