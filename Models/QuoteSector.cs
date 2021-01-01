namespace SabreWebtopTicketingService.Models
{
    public class QuoteSector
    {
        public int PQSectorNo { get; set; }
        public bool ConnectionIndicator { get; set; }
        public string DepartureCityCode { get; set; }
        public string ArrivalCityCode { get; set; }
        public string DepartureDate { get; set; }
        public string FareBasis { get; set; }
        public string NVB { get; set; }
        public string NVA { get; set; }
        public string Baggageallowance { get; set; }
        public bool Void { get; set; }
        public bool Arunk { get; set; }
    }
}