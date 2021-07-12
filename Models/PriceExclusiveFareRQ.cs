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
    public class PriceExclusiveFareRQ
    {
        public string warmer { get; set; }
        public string SessionID { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public List<PriceExclusiveFarePassenger> SelectedPassengers { get; set; }
        public string ExclusivePCC { get; set; }
        public string PriceCode { get; set; }
        public string PlatingCarrier { get; set; }
        public bool AlternativePricing { get; set; }
        public bool IsRTW { get; set; }
        
    }


    //Fluent Validation
    public class PriceExclusiveFareRQValidator : AbstractValidator<GetQuoteRQ>
    {
        public PriceExclusiveFareRQValidator()
        {
            RuleFor(x => x.Locator).NotNull().NotEmpty().Length(6).WithMessage("Locator not found or not in valid format").WithErrorCode("10000001");
            RuleFor(x => x.GDSCode).NotNull().Length(2).Matches(@"^\d[A-Z]").WithMessage("GDS Code not found or not in valid format").WithErrorCode("10000002");
            RuleFor(x => x.SelectedPassengers).NotNull().NotEmpty().WithMessage("Passengers must be selected before quoting").WithErrorCode("10000003");
            RuleFor(x => x.SelectedSectors).NotNull().WithMessage("Sectors must be selected before quoting").WithErrorCode("10000004");
        }
    }

    public class PriceExclusiveFarePassenger
    {
        public string NameNumber { get; set; }
        public string PaxType { get; set; }
        public FOP FormOfPayment { get; set; }
    }
}
