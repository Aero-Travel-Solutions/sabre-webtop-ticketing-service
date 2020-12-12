using System.Collections.Generic;
using System.Text.Json;

namespace SabreWebtopTicketingService.Models
{
    public class BackofficeOptions
    {
        public string EnabledBackoffice { get; set; }
        public Dictionary<string, Process> ConsolidatorsBackofficeProcess => JsonSerializer.Deserialize<Dictionary<string, Process>>(EnabledBackoffice);
    }

    public class Process
    {
        public bool CreditLimitCheck { get; set; }
        public bool DownloadDocuments { get; set; }
        public bool DownloadVoidDocuments { get; set; }
        public bool DownloadRefunds { get; set; }
    }
}
