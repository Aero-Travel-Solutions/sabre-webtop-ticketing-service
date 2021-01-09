using FluentValidation;
using SabreWebtopTicketingService.Common;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{
    public class PNR
    {
        public string GDSCode { get; set; }
        public string Locator { get; set; }
        public string BookedPCC { get; set; }
        public string BookedPCCCityCode { get; set; }
        public string CreatedDate { get; set; }
        public string BookingTTL { get; set; }
        public bool ExpiredQuotesExist { get; set; }
        public bool UnconfirmedSectorsExist => !Sectors.IsNullOrEmpty() && Sectors.
                                                        Any(a => !(a.Status.IsNullOrEmpty() || "HK,KK,KL,RR,TK,EK".Contains(a.Status)));
        public List<PNRSector> Sectors { get; set; }
        public List<PNRPassengers> Passengers { get; set; }
        public List<PNRStoredCards> StoredCards { get; set; }
        public List<Quote> Quotes { get; set; }
        public List<Ancillary> Ancillaries { get; set; }
        public List<SSR> SSRs { get; set; }
        public List<PNRTicket> Tickets { get; set; }
        public List<WebtopWarning> Warnings { get; set; }
        public int LastQuoteNumber { get; set; }
        public string DKNumber { get; set; }       
        public string HostUserId { get; set; }
        public List<PNRAgent> Agents { get; set; }
     
        public void Dispose()
        {

        }
    }

    public class PNRAgent
    {
        public string AgentId { get; set; }
        public string Name { get; set; }
    }

    public class PNRValidator : AbstractValidator<PNR>
    {
        public PNRValidator()
        {
            RuleFor(x => x.Sectors).NotNull().
                NotEmpty().
                WithMessage("No sectors found!").
                WithErrorCode("10000003");
            
            RuleFor(x => x.Passengers).
                NotNull().
                NotEmpty().
                WithMessage("No passengers found!").
                WithErrorCode("10000004");
        }
    }
}
