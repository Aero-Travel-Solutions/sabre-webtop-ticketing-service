using System;
using System.Collections.Generic;
using System.Text;

namespace Aeronology.DTO
{
    public class FreeSeat
    {
        public string SeatNumber { get; set; }
        public bool NoSmoking { get; set; }
        public string SeatType { get; set; }
        public string Status { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Changed { get; set; }
        public string NameNumber { get; set; }
        public int SectorNo { get; set; }
    }
}
