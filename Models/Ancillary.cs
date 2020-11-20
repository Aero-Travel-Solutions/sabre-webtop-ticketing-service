using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class Ancillary
    {
        public string AncillaryGroup { get; set; }
        public int EMDNumber { get; set; }
        public string ID { get; set; }
        public string CommercialName { get; set; }
        public string SeatNumber { get; set; }
        public string RFISC { get; set; }
        public string RFIC { get; set; }
        public decimal BasePrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string CurrencyCode { get; set; }
        public string PassengerName { get; set; }
        public string NameNumber { get; set; }
        public string Carrier { get; set; }
        public List<AncillarySector> Sectors { get; set; }
        public string Route => Origin + "-" + Destination;
        public string Origin { get; set; }
        public string Destination { get; set; }
        public List<Tax> Taxes { get; set; }
        public decimal TotalTax { get; set; }
        public string InvalidErrorCode { get; set; }
        public bool AlreadyTicketed { get; set; }
        public bool TicketAssociated { get; set; }
        public decimal? Commission { get; set; }
        public decimal? GST => Taxes?.FirstOrDefault(f => f.Code == "UO")?.Amount;
        public bool IsGST => GST.HasValue;
        public decimal? Fee { get; set; }
        public decimal? FeeGST => IsGST? Fee * (GSTRate.HasValue? (GSTRate.Value / 100) : default(decimal?)) : default;
        [JsonIgnore]
        public decimal? GSTRate { get; set; }
        public decimal? AgentCommissionRate { get; set; }
        public decimal? BspCommissionRate { get; set; }
        public decimal DefaultCreditCardAmount => TotalPrice;
        public decimal DefaultPriceItAmount => TotalPrice + (Fee.HasValue ? Fee.Value : 0.00M) + (FeeGST.HasValue ? FeeGST.Value : 0.00M);
        public bool UnpaidSeat { get; set; }
        public List<string> Warnings { get; set; }
        public string IssueTicketEMDKey { get; set; }
        public string PurchaseByDate { get; set; }
        public decimal AgentPrice => TotalPrice + (Fee.HasValue ? Fee.Value : 0.00M) + (FeeGST.HasValue ? FeeGST.Value : 0.00M) - (Commission.HasValue ? Commission.Value : 0.00M);
    }

    public class AncillarySector
    {
        public int SectorNo { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string FlightNo { get; set; }
        public string DepartureDate { get; set; }
        public string MarketingCarrier { get; set; }
        public string OperatingCarrier { get; set; }
    }
}
