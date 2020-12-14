using SabreWebtopTicketingService.Common;
using System.Collections.Generic;
using System.Linq;
using VoidTicket;

namespace SabreWebtopTicketingService.Models
{
    public class SabreVoidTicketResponse
    {
        VoidTicketRQResponse res = null;
        List<string> errorlist = new List<string>();
        string locator = "";

        public SabreVoidTicketResponse(string documentNumber, string documenttype, VoidTicketRQResponse rs, string locator,  string error = "", bool alreadyvoided = false)
        {
            res = rs;
            DocumentNumber = documentNumber;
            DocumentType = documenttype;
            this.locator = locator;
            if (!string.IsNullOrEmpty(error)) { errorlist.Add(error); }
            this.Alreadyvoided = alreadyvoided;
        }

        internal VoidTicketRS voidTicketRS => res?.VoidTicketRS;

        public bool Alreadyvoided { get; set; }
        public bool Success
        {
            get
            {
                bool result = voidTicketRS != null &&
                               voidTicketRS.ApplicationResults.status == CompletionCodes.Complete &&
                               voidTicketRS.Text.Any(s => s.Contains("VOID MSG SENT"));

                if (!errorlist.IsNullOrEmpty())
                {
                    List<string> excludederrors = new List<string>()
                    {
                        "ERROR NUMBER PREVIOUSLY VOIDED",
                        "NUMBER PREVIOUSLY VOIDED-VERIFY NBR ENTERED"
                    };

                    var exerr = errorlist.
                                    Where(w => excludederrors.Contains(w));

                    if(!exerr.IsNullOrEmpty())
                    {
                        errorlist = new List<string>() { $"Ticket {DocumentNumber} already voided." };
                        Alreadyvoided = true;
                        result = true;
                    }
                }
                return result;
            }
        }

        public List<string> Errors
        {
            get
            {
                if (voidTicketRS != null && voidTicketRS.ApplicationResults.Error != null)
                {
                    var errors = voidTicketRS.
                                    ApplicationResults.
                                    Error?.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message).
                                    Select(s => s.Value).
                                    Distinct().
                                    ToList();

                    errorlist.
                        AddRange(errors);

                    errorlist = errorlist.Distinct().ToList();
                }

                return errorlist;
            }
            set { }
        }


        public string DocumentNumber { get; private set; }
        public string DocumentType { get; private set; }
    }
}
