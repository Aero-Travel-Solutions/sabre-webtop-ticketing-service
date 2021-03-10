using FluentValidation;
using Newtonsoft.Json;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{
    public class GetQuoteRQ
    {
        public string warmer { get; set; }
        private List<QuotePassenger> paxs =null;
        public string SessionID { get; set; }
        public string AgentID { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public List<SelectedPassengerData> SelectedPassengerKeys { get; set; }
        [JsonIgnore]
        public List<QuotePassenger> SelectedPassengers
        {
            get
            {
                if (!SelectedPassengerKeys.IsNullOrEmpty())
                {
                    paxs = new List<QuotePassenger>();
                    foreach (var SelectedPaxKey in SelectedPassengerKeys)
                    {
                        QuotePassenger quotePassenger = new QuotePassenger();
                        string key = "";
                        if (!string.IsNullOrEmpty(SelectedPaxKey.PassengerKey))
                        {
                            key = SelectedPaxKey.PassengerKey.DecodeBase64();
                            quotePassenger = JsonConvert.DeserializeObject<QuotePassenger>(key);

                            if (quotePassenger == null)
                            {
                                throw new AeronologyException("QUOTE_PAX_NOT_FOUND", "Invalid Request");
                            }

                            if (!string.IsNullOrEmpty(SelectedPaxKey.PaxType))
                            {
                                quotePassenger.PaxType = SelectedPaxKey.PaxType;
                            }

                            if (SelectedPaxKey.DOBChanged)
                            {
                                quotePassenger.DOB = SelectedPaxKey.DOB;
                            }

                            if (SelectedPaxKey.FormOfPayment != null)
                            {
                                quotePassenger.FormOfPayment = SelectedPaxKey.FormOfPayment;
                            }

                            if (SelectedPaxKey.PriceIt != decimal.MinValue)
                            {
                                quotePassenger.PriceIt = SelectedPaxKey.PriceIt;
                            }
                        }
                        paxs.Add(quotePassenger);
                    }
                }

                return paxs;
            }

            set 
            { 
                paxs = value; 
            }
        }
        public List<SelectedQuoteSector> SelectedSectors { get; set; }
        public string PriceCode { get; set; }
        public string PlatingCarrier { get; set; }
        public bool AlternativePricing { get; set; }
        public bool IsRTW { get; set; }
        public DateTime? QuoteDate { get; set; }
    }

    public class SelectedQuoteSector
    {
        public int SectorNo { get; set; }
    }

    //Fluent Validation
    public class GetQuoteRQValidator : AbstractValidator<GetQuoteRQ>
    {
        public GetQuoteRQValidator()
        {
            RuleFor(x => x.Locator).NotNull().NotEmpty().Length(6).WithMessage("Locator not found or not in valid format").WithErrorCode("10000001");
            RuleFor(x => x.GDSCode).NotNull().Length(2).Matches(@"^\d[A-Z]").WithMessage("GDS Code not found or not in valid format").WithErrorCode("10000002");
            RuleFor(x => x.SelectedPassengers).NotNull().NotEmpty().WithMessage("Passengers must be selected before quoting").WithErrorCode("10000003");
            RuleFor(x => x.SelectedSectors).NotNull().WithMessage("Sectors must be selected before quoting").WithErrorCode("10000004");
        }
    }
}
