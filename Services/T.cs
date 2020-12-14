using SabreWebtopTicketingService.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Services
{
    internal class T
    {
        /*
          TKT/TIME LIMIT
          1.T-11DEC-9SNJ*AWS
          2.TE 0819473963003-AU ALI/M 9SNJ*AWS 1245/11DEC*I
            TV 0819473963003-ET  *VOID* 9SNJ*AAF 1317/11DEC*E
          3.TE 0819473963004-AU ALI/M 9SNJ*AWS 1444/11DEC*I
        */
        string text = "";

        public T(string gdstext)
        {
            text = gdstext;
        }

        public List<TicketData> Tickets
        {
            get
            {
                List<TicketData> tktdata = new List<TicketData>();
                var tkts = text.SplitOn("\n").Skip(2);

                var tktline = tkts.Where(w => w.IsMatch(@"^\s*\d+\.[TM][E]\s+\d{13}"));
                var voidlines = tkts.Where(w => w.IsMatch(@"^\s+[TM][V]\s+\d{13}"));

                tktdata = tktline.
                            Select(t => new TicketData()
                            {
                                DocumentNumber = t.LastMatch(@"\d{13}"),
                                RPH = int.Parse(t.LastMatch(@"^\s*(\d+)\.[TM][E]\s+\d{13}")),
                                Voided = voidlines.FirstOrDefault(f=> f.Contains(t.LastMatch(@"\d{13}"))) != null,
                                DocumentType = t.LastMatch(@"^\s*\d+\.([TM])[E]\s+\d{13}") == "T" ? "TKT" : "EMD",
                                PassengerName = t.LastMatch(@"\d{13}-[AETU]{2}\s([A-Z\/]*)\s"),
                                TicketingPCC = t.LastMatch(@"\d{13}-[AETU]{2}\s[A-Z\/]*\s*(\w{4})\*[A-Z]{3}\s*\d{3,4}\/\d{1,2}[A-Z]{3}[\*\s]*[A-Z]"),
                                IssueDate = t.LastMatch(@"\d{3,4}\/(\d{1,2}[A-Z]{3})[\*\s]*[A-Z]"),
                                IssueTime = t.LastMatch(@"(\d{3,4})\/\d{1,2}[A-Z]{3}[\*\s]*[A-Z]")
                            }).
                            ToList();

                return tktdata;
            }
        }
    }

    public class TicketData
    {
        public string DocumentNumber { get; set; }
        public string DocumentType { get; set; }
        public int RPH { get; set; }
        public bool Voided { get; set; }
        public string PassengerName { get; set; }
        public string TicketingPCC { get; set; }
        public string IssueDate { get; set; }
        public string IssueTime { get; set; }
        public string IssueDateFormatted
        {
            get
            {
                DateTime temsissuedate = DateTime.ParseExact(IssueDate, "ddMMM", System.Globalization.CultureInfo.InvariantCulture);
                return temsissuedate.ToString("MM-dd");
            }
        }

    }
}
