using FluentValidation;
using Newtonsoft.Json;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Interface;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{
    public class ValidateCommissionRQ
    {
        public string SessionID { get; set; }
        public string AgentID { get; set; }
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public List<PNRSector> Sectors { get; set; }
        public List<Quote> Quotes {get;set;}
    }

    //Fluent Validation
    public class ValidateCommissionRQValidator : AbstractValidator<ValidateCommissionRQ>
    {
        public ValidateCommissionRQValidator()
        {
            RuleFor(x => x.GDSCode).NotNull().Length(2).Matches(@"^\d[A-Z]").WithMessage("GDS Code not found or not in valid format.").WithErrorCode("GDS_NOT_FOUND");
            RuleFor(x => x.Quotes).NotNull().NotEmpty().WithMessage("At least one quote is required to proceed.").WithErrorCode("QUOTE_NOT_FOUND");
            RuleFor(x => x.Sectors).NotNull().NotEmpty().WithMessage("At least one sector is required to proceed.").WithErrorCode("SECTORS_NOT_FOUND");

        }
    }
}
