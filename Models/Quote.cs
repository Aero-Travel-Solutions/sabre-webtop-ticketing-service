using Newtonsoft.Json;
using SabreWebtopTicketingService.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{
    public class Quote
    {
        private decimal? agtcommrate = 0;

        public int QuoteNo { get; set; }
        public PriceType PriceType { get; set; }
        public FareType FareType { get; set; }
        public string LastPurchaseDate { get; set; }
        public bool FiledFare { get; set; }
        public QuotePassenger QuotePassenger { get; set; }
        public List<QuoteSector> QuoteSectors { get; set; }
        public decimal BaseFare { get; set; }
        public decimal EquivFare { get; set; }
        public string EquivFareCurrencyCode { get; internal set; }
        public decimal CreditCardFee { get; set; } = 0.00M;
        public decimal CreditCardFeeRate { get; set; }
        public decimal? GST => Taxes?.FirstOrDefault(f => f.Code == "UO")?.Amount;
        public bool IsGST => GST.HasValue;
        public decimal? FeeGST => IsGST && GSTRate.HasValue ? (Fee * GSTRate.Value) / 100 : default(decimal?);
        public decimal TotalFare => BaseFare + TotalTax;
        public decimal AgentPrice { get; set; }
        public string BaseFareCurrency { get; set; }
        public decimal TotalGrossPrice => BaseFare + TotalTax + Commission + Fee + (FeeGST.HasValue ? FeeGST.Value : 0.00M);
        public decimal DefaultPriceItAmount => TotalFare + Fee + (FeeGST.HasValue ? FeeGST.Value : 0.00M);
        public decimal DefaultCreditCardAmount => TotalFare;
        public decimal TotalTax => Taxes?.Sum(s => s.Amount) ?? 0.00M;
        public List<Tax> Taxes { get; set; }
        public string FareCalculation { get; set; }
        public string TurnaroundPoint { get; set; }
        public string PlatingCarrier { get; set; }
        public List<string> Endorsements { get; set; }
        public string NonRefundable { get; set; }
        public bool PrivateFare { get; set; }
        public string ROE { get; set; }
        public List<SpecificPenalty> SpecificPenalties { get; set; }
        public bool Expired { get; set; }
        public List<WebtopWarning> Warnings { get; set; }
        public List<WebtopError> Errors {get;set;}
        public int SectorCount { get; set; }
        public string Route { get; set; }
        public string IssueTicketQuoteKey { get; set; }
        public string PricingCommand { get; set; }
        public string PriceCode { get; set; }
        public string TourCode { get; set; }
        public bool CAT35 => !string.IsNullOrEmpty(TourCode);
        [JsonIgnore]
        public List<string> DifferentPaxType { get; set; }
        public string TicketingPCC { get; set; }
        public string PricingHint { get; set; }
        public string CCFeeData { get; set; }

        //Commision and fees
        public string ContextID { get; set; }
        public decimal? AgentCommissionRate 
        {
            get
            {
                if (AgentCommissions != null)
                {
                    agtcommrate = AgentCommissions.FirstOrDefault()?.AgtCommRate;
                }
                return agtcommrate;
            }

            set
            {
                agtcommrate = value;
            }
        }
        public decimal? BspCommissionRate { get; set; }
        [JsonIgnore]
        public decimal? GSTRate { get; set; }
        public decimal? FuelSurcharge => AgentCommissions?.FirstOrDefault()?.FuelSurcharge;
        public decimal? CommissionGST => IsGST && GSTRate.HasValue ? (Commission * (GSTRate.Value / 100)) : default(decimal?);
        public decimal Commission => AgentCommissions.IsNullOrEmpty() || !AgentCommissions.First().AgtComm.Amount.HasValue ?
                                            0.00M:
                                            AgentCommissions.First().AgtComm.Amount.Value;
        public decimal Fee { get; set; }
        public List<AgentCommission> AgentCommissions { get; set; }
    }

    public enum PriceType
    {
        Published,
        Manual
    }

    public enum FareType
    {
        Published,
        IT,
        BT,
        NR
    }
}