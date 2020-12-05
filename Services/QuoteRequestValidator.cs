
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;

namespace SabreWebtopTicketingService.Services
{
    public static class QuoteRequestValidator
    {
        public static void Validate(this GetQuoteRQ getQuoteRQ)
        {
            if (getQuoteRQ.SelectedPassengers.IsNullOrEmpty())
            {
                throw new AeronologyException("NO_PAX_SELECT", "No passengers selected");
            }

            if (getQuoteRQ.SelectedSectors.IsNullOrEmpty())
            {
                throw new AeronologyException("NO_SEG_SELECT", "No sectors selected");
            }
        }

        public static void Validate(this ForceFBQuoteRQ forcefbRQ)
        {
            if (forcefbRQ.SelectedPassengers.IsNullOrEmpty())
            {
                throw new AeronologyException("NO_PAX_SELECT", "No passengers selected");
            }

            if (forcefbRQ.SelectedSectors.IsNullOrEmpty())
            {
                throw new AeronologyException("NO_SEG_SELECT", "No sectors selected");
            }
        }
    }
}
