using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Services
{
    //WI - TICKET FARE AMOUNT MASK - DEPRESS ENTER TO CONTINUE OR
    //RESET AND CLEAR TO RETURN TO PNR
    //TKT RECORD NBR <2>
    //PASSENGER TYPE ADT

    //ENDORSEMENT ENTER X IF SUBJ GOVT APRVL<>
    //<>


    //BASE FARE - CURRENCY CODE/AMOUNT <><>
    //INTL EQUIV CURRENCY/AMOUNT IF APPLICABLE <><>

    //TAX AMOUNT/CODE 1<><> TAX AMOUNT/CODE 2<><>
    //TAX AMOUNT/CODE 3<><> TAX AMOUNT/CODE 4<><>
    //TAX AMOUNT/CODE 5<><> TAX AMOUNT/CODE 6<><>
    //ENTER X IF MORE THAN 6 TAXES <> IF ALL TAXES EXEMPT ENTER X<>
    //COMMISSION PCT <> TOUR CODE<>
    //OR AMT<>
    internal class SabreManualBuildScreen1
    {
        string mask = "";
        IssueExpressTicketQuote quote;
        public SabreManualBuildScreen1(string resp, IssueExpressTicketQuote quote)
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

                //TKT RECORD RECORD
                if (Lines.Any(a => a.Contains("TKT RECORD RECORD")))
                {
                    string value = Lines.First(f => f.IsMatch(@"TKT\RECORD\sRECORD\s+<")).LastMatch(@"TKT\RECORD\sRECORD\s+<([\s\d]*)>");
                    returncommand += string.IsNullOrWhiteSpace(value) ? "<>" : $"<{value.Trim()}>";
                }

                //ENDORSEMENT
                returncommand += quote.Endorsements.IsNullOrEmpty() ? "<>" : $"<{string.Join(" ", quote.Endorsements)}>";

                //IF SUBJ GOVT APRVL
                returncommand += "<>";

                //BASE FARE
                returncommand += $"<{quote.BaseFareCurrency}><{quote.BaseFare}";

                //EQUIV FARE
                if (quote.EquivFare.HasValue)
                {
                    returncommand += $"<{quote.EquivFareCurrency}><{quote.EquivFare}";
                }

                //TAX
                if (quote.Taxes.IsNullOrEmpty())
                {
                    returncommand += "<><><><><><><><><><><><>";
                }
                else
                {
                    var taxitems = quote.
                                        Taxes.
                                        GroupBy(grp => grp.Code).
                                        Select(t => new Tax()
                                        {
                                            Code = t.Key,
                                            Amount = t.Sum(ts => ts.Amount)
                                        }).
                                        Take(6).
                                        Select(tax => $"<{tax.Amount}><{tax.Code}>").
                                        ToList();

                    if (taxitems.Count() < 6)
                    {
                        int missingitemcount = 6 - taxitems.Count();
                        for (int i = 0; i < missingitemcount; i++)
                        {
                            taxitems.
                                Add("<><>");
                        }
                    }

                    returncommand += string.Join("", taxitems);

                    //ENTER X IF MORE THAN 6 TAXES
                    returncommand += quote.Taxes.GroupBy(grp => grp.Code).Count() > 6 ? "<X>" : "<>";
                }

                //IF ALL TAXES EXEMPT ENTER
                returncommand += "<>";

                //COMMISSION PCT
                returncommand += quote.BSPCommissionRate.HasValue ? $"<{quote.BSPCommissionRate}>" : "<>";

                //TOUR CODE
                returncommand += string.IsNullOrEmpty(quote.TourCode) ? "<>" : $"<{quote.TourCode.Trim().ToUpper()}>";

                //OR AMT
                returncommand += "<>";

                return returncommand;
            }

            internal set
            {
            }
        }

        public bool AdditionalTaxPresent => quote.
                                                Taxes.
                                                GroupBy(grp => grp.Code).Count() > 6;
    }
}
