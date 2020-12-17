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
    public class SabreManualBuildAdditinalTax
    {
        string mask = "";
        List<Tax> taxes;
        public SabreManualBuildAdditinalTax(string resp, List<Tax> taxes)
        {
            mask = resp;
            this.taxes = taxes;

            if (!Success)
            {
                throw new GDSException("UNKNOWN_MASK", mask);
            }
        }

        public bool Success => mask.IsMatch("FARE AMOUNT MASK - DEPRESS ENTER TO CONTINUE") && mask.Contains("TAX AMOUNT/CODE");


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

                //TAX
                if(taxes.IsNullOrEmpty())
                {
                    returncommand += "<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>";
                }
                else
                {
                    var taxitems = taxes.
                                    Select(tax => $"<{tax.Amount}><{tax.Code}>").
                                    ToList();
                    
                    if(taxitems.Count()< 22)
                    {
                        int missingitemcount = 22 - taxitems.Count();
                        for (int i = 0; i < missingitemcount; i++)
                        {
                            taxitems.
                                Add("<><>");
                        }  
                    }

                    returncommand += string.Join("", taxitems);

                    //IF MORE TAXES
                    returncommand +=taxes.Count() > 22 ? "<X>" : "<>";
                }

                return returncommand;
            }

            internal set
            {
            }
        }

        public bool AdditionalTaxPresent => taxes.Count() > 22;
    }
}
