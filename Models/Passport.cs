using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class Passport
    {
        public string PasspoerNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string IssueCountry { get; set; }
        public string NationalityCountry { get; set; }
    }
}
