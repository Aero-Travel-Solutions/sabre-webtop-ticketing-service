﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class SearchPNRResponse
    {
        public PNR PNR { get; set; }
        public List<WebtopError> Errors { get; set; }
    }
}
