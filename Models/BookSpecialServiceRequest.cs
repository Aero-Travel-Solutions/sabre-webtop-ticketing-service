using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class BookSpecialServiceRequest
    {
        public string Locator { get; set; }
        public List<ServiceData> ServiceData{get;set;}
    }
    public class ServiceData
    {
        public string SSRCode { get; set; }
        public string NameNumber { get; set; }
        public string SegmentNumber { get; set; }
        public bool AllSectors { get; set; }
        public string Carrier { get; set; }
        public string SpecialText { get; set; }
        public bool OSI { get; set; }
        public PassengerDetail PassengerDetails { get; set; }
    }
}
