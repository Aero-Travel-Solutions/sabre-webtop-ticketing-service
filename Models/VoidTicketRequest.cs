using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class VoidTicketRequest
    { 
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public List<VoidTicket> Tickets { get; set; }
    }

    public class VoidTicketRequestValidator : AbstractValidator<VoidTicketRequest>
    {
        public VoidTicketRequestValidator()
        {
            
            RuleFor(x => x.Locator).NotNull().Matches(@"\w{6}").MaximumLength(6);
            RuleFor(x => x.Tickets).NotNull().NotEmpty().Must(o => o.Count > 0).WithMessage("No tickets found tp void");
        }
    }
}
