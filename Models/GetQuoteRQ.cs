using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class GetQuoteRQ
    {
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public string AccessKey { get; set; }
        public List<QuotePassenger> SelectedPassengers { get; set; }
        public List<int> SelectedSectors { get; set; }
        public string PriceCode { get; set; }
        public bool AlternativePricing { get; set; }
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
