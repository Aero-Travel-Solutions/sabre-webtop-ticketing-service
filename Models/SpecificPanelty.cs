namespace SabreWebtopTicketingService.Models
{
    public class SpecificPenalty
    {
        public bool SourcedFromCAT16 { get; set; }
        public string FareBasis { get; set; }
        public decimal PenaltyAmount { get; set; }
        public string CurrencyCode { get; set; }
        public string PenaltyTypeDescription { get; set; }
        public PenaltyType PenaltyTypeCode { get; set; }
        public string FareComponent { get; set; }
    }

    public enum PenaltyType
    {
        NONE,
        CPBD,//Change Penalty Before Departure
        CPAD,//Change Penalty After Departure
        RPBD,//Refund Penalty Before Departure
        RPAD //Refund Penalty After Departure
    }
}