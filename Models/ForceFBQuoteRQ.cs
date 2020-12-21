using FluentValidation;
using Newtonsoft.Json;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class ForceFBQuoteRQ
    {
        public string SessionID { get; set; }
        public string AgentID { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public string AccessKey { get; set; }
        public List<SelectedPassengerData> SelectedPassengerKeys { get; set; }
        [JsonIgnore]
        public List<QuotePassenger> SelectedPassengers
        {
            get
            {
                List<QuotePassenger> quotePassengers = new List<QuotePassenger>();
                if (!SelectedPassengerKeys.IsNullOrEmpty())
                {
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

                            if(SelectedPaxKey.PriceIt != decimal.MinValue)
                            {
                                quotePassenger.PriceIt = SelectedPaxKey.PriceIt;
                            }
                        }
                        quotePassengers.Add(quotePassenger);
                    }
                }

                return quotePassengers;
            }

            private set { }
        }
        public List<SelectedSector> SelectedSectors { get; set; }
        public string PriceCode { get; set; }
        public string PlatingCarrier { get; set; }
        public bool AlternativePricing { get; set; }
    }

    public class SelectedSector
    {
        public int SectorNo { get; set; }
        public string FareBasis { get; set; }
        public string TicketDesignator { get; set; }
    }

    //Fluent Validation
    public class ForceFBQuoteRQValidator : AbstractValidator<GetQuoteRQ>
    {
        public ForceFBQuoteRQValidator()
        {
            RuleFor(x => x.Locator).NotNull().NotEmpty().Length(6).WithMessage("Locator not found or not in valid format").WithErrorCode("10000001");
            RuleFor(x => x.GDSCode).NotNull().Length(2).Matches(@"^\d[A-Z]").WithMessage("GDS Code not found or not in valid format").WithErrorCode("10000002");
            RuleFor(x => x.SelectedPassengers).NotNull().NotEmpty().WithMessage("Passengers must be selected before quoting").WithErrorCode("10000003");
            RuleFor(x => x.SelectedSectors).NotNull().WithMessage("Sectors must be selected before quoting").WithErrorCode("10000004");
        }
    }
}
