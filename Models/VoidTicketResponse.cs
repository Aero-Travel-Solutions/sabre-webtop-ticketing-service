using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class VoidTicketResponse
    {
        public string DocumentNumber { get; set; }
        [JsonIgnore]
        public string DocumentType { get; set; }
        public bool Voided { get; set; }
        public bool AlreadyVoided { get; set; }
        public List<string> Errors { get; set; }
    }
}
