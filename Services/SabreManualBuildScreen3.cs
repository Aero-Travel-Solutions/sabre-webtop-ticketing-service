using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System.Linq;

namespace SabreWebtopTicketingService.Services
{
    internal class SabreManualBuildScreen3
    {
        string mask = "";
        IssueExpressTicketQuote quote;
        public SabreManualBuildScreen3(string resp, IssueExpressTicketQuote quote)
        {
            mask = resp;
            this.quote = quote;

            if (!Success)
            {
                throw new GDSException("UNKNOWN_MASK", mask);
            }
        }

        internal bool Success => mask.IsMatch("FARE AMOUNT MASK - DEPRESS ENTER TO CONTINUE");

        internal string[] Lines => mask.SplitOn("\n");

        public string Command
        {
            get
            {
                string returncommand = "WI";

                returncommand += string.
                                    Join("",
                                        quote.
                                        Sectors.
                                        Where(w => !w.Arunk && !w.Void).
                                        Select(s => new
                                        {
                                            connectionindicator = s.ConnectionIndicator ? "<X>" : "<>",
                                            s.NVA,
                                            s.NVB,
                                            s.Baggageallowance,
                                            s.FareBasis
                                        }).
                                        Select(s => $"<{s.connectionindicator}><{s.NVB}><{s.NVA}><{s.Baggageallowance}><{s.FareBasis}>"));

                //FARE CALCULATION
                if (Lines.Any(a => a.Contains("FARE CALCULATION")))
                {
                    returncommand += string.Join("", quote.FareCalculation.Trim().SplitInChunk(59).Select(s => $"<{s}>"));
                }

                return returncommand;
            }

            internal set
            {
            }
        }
    }
}