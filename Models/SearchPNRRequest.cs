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
        public string QueueID { get; set; }
        public string warmer { get; set; }
    }



    //Fluent Validation
    public class SearchPNRRequestValidator : AbstractValidator<SearchPNRRequest>
    {
        public SearchPNRRequestValidator()
        {
            RuleFor(x => x.GDSCode).NotNull().NotEmpty().Matches(@"\d[A-Z]").WithMessage("GDS Code not found or not in valid format").WithErrorCode("GDS_CODE_NOT_FOUND");
            RuleFor(x => x.SearchText).NotNull().NotEmpty().WithMessage("Search criteria must be specified").WithErrorCode("SEARCH_TEXT_NOT_FOUND");
        }
    }
}
