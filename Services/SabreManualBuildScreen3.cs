using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System.Linq;

namespace SabreWebtopTicketingService.Services
{
    //WI - TICKET FARE INFO - DEPRESS ENTER WHEN COMPLETE
    //                                 NOT VALID  NOT VALID      BAG
    //                                  BEFORE      AFTER ALLOW
    // 01 <O> MEL QF   29 V 01SEP OK<       >  <       >     <   >
    //                         FARE BASIS/TKT DESIG <               >
    // 02 < > HKG<>  <       >     <   >
    //                         FARE BASIS/TKT DESIG <VOID           >
    // 03 <O> SIN QF   36 L 15SEP OK   <       >  <       >     <   >
    //        MEL FARE BASIS/TKT DESIG <               >





    //FARE CALCULATION - LEAVE BLANK TO BUILD AUTO FARE CALCULATION
    //<                                                             >
    //<                                                             >
    //<                                                             >
    //<                                                             >

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

        internal bool Success => mask.IsMatch("TICKET  FARE INFO - DEPRESS ENTER WHEN COMPLETE");

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
                                        Select(s => new
                                        {
                                            connectionindicator = s.Arunk ?  "<>":"<O>",
                                            NVA = s.Arunk ? "" : s.NVA,
                                            NVB = s.Arunk ? "" : s.NVB,
                                            Baggageallowance = s.Arunk ? "" : s.Baggageallowance,
                                            FareBasis = s.Arunk ? "VOID" : s.FareBasis
                                        }).
                                        Select(s => $"<{s.connectionindicator}><{s.NVB}><{s.NVA}><{s.Baggageallowance}><{s.FareBasis}>"));

                //FARE CALCULATION
                if (Lines.Any(a => a.Contains("FARE CALCULATION")))
                {
                    returncommand += string.Join("", quote.FareCalculation.Trim().SplitInChunk(246).Select(s => $"<{s}>"));
                }

                return returncommand;
            }

            internal set
            {
            }
        }
    }
}