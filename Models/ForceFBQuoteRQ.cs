using FluentValidation;
using SabreWebtopTicketingService.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class ForceFBQuoteRQ: IQuoteRequest
    {
        public string SessionID { get; set; }
        public string AgentID { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public string AccessKey { get; set; }
        public List<QuotePassenger> SelectedPassengers { get; set; }
        public List<IQuoteSector> SelectedSectors { get; set; }
        public string PriceCode { get; set; }
        public bool AlternativePricing { get; set; }
    }

    public class SelectedSector: IQuoteSector
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
