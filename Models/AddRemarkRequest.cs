using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class AddRemarkRequest
    {
        public string Locator { get; set; }
        public List<Remark> Remarks { get; set; }
    }

    public class Remark
    {
        public string Code { get; set; }
        public string RemarkText { get; set; }
        public string SegmentNumber { get; set; }
    }
}
