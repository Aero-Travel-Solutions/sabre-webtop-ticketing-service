using FluentValidation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class GetQuoteTextRequest
    {
        [DefaultValue("1W")]
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public bool AllQuotes { get; set; }
        public bool AllEMDs { get; set; }
        public int? QuoteNo { get; set; }
        public int? EMDNo { get; set; }
    }


    //Fluent Validation
    public class GetQuoteTextRequestValidator : AbstractValidator<GetQuoteTextRequest>
    {
        public GetQuoteTextRequestValidator()
        {
            RuleFor(x => x.Locator).NotNull().NotEmpty().Length(6).WithMessage("Locator not found or not in valid format").WithErrorCode("10000001");
            RuleFor(x => x.GDSCode).NotNull().Length(2).Matches(@"^\d[A-Z]").WithMessage("GDS Code not found or not in valid format").WithErrorCode("10000002");
        }
    }

    public enum TextDocumentType
    {
        QUOTE,
        EMD
    }
}
