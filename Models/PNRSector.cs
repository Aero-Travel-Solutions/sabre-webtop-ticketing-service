namespace SabreWebtopTicketingService.Models
{
    public class PNRSector
    {
        public int SectorNo { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Carrier { get; set; }
        public string Flight { get; set; }
        public string Class { get; set; }
        public string DepartureDate { get; set; }
        public string DepartureTime { get; set; }
        public string Status { get; set; }
        public string Equipment { get; set; }
        public bool Flown { get; set; }
        public bool IsArunk => From == "ARUNK";
        public bool Unconfirmed => string.IsNullOrEmpty(Status) ? false : "UN|UC".Contains(Status);
        public decimal Mileage { get; set; }
        public bool SecureFlightDataRequired { get; set; }
        public string ArrivalDate { get; set; }
        public string ArrivalTime { get; set; }
        public string OperatingCarrier { get; set; }
        public string OperatingCarrierFlightNo { get; set; }
        public string Cabin { get; set; }
        public string CabinDescription { get; set; }
        public string AirlineRecordLocator { get; set; }
        public bool CodeShare { get; set; }
        public string MarriageGroup { get; set; }
        public string FlightDuration { get; set; }
        public string SeatMapSectorKey { get; set; }
        public bool Ticketed { get; set; }
    }
}