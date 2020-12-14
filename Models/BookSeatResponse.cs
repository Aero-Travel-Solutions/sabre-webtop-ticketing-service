using FluentValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class BookSeatResponse
    {
        public string Locator { get; set; }
        public List<SeatBooked> Seats { get; set; }
        public List<Ancillary> Ancillaries { get; set; }
    }

    public class SeatBooked
    {
        public string SeatNumber { get; set; }
        public string NameNumber { get; set; }
        public string SegmentNumber { get; set; }
        public bool Success { get; set; }
        public bool EMDBooked { get; set; }
        public List<string> Warnings { get; set; }
    }
}
