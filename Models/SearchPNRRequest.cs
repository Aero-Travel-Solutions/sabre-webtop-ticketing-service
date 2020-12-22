using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class SearchPNRRequest
    {
        public string SessionID { get; set; }        
        public string GDSCode { get; set; }
        public string SearchText { get; set; }
        public string AgentID { get; set; }
    }



    //Fluent Validation
    public class SearchPNRRequestValidator : AbstractValidator<SearchPNRRequest>
    {
        public SearchPNRRequestValidator()
        {
            RuleFor(x => x.GDSCode).NotNull().NotEmpty().Matches(@"\d[A-Z]").WithMessage("GDS Code not found or not in valid format").WithErrorCode("10000002");
            RuleFor(x => x.SearchText).NotNull().NotEmpty().WithMessage("Search criteria must be specified").WithErrorCode("10000003");
        }
    }
}
