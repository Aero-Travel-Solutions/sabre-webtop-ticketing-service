using GetReservation;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{
    internal class SabreFrequentFlyer
    {
        private FrequentFlyerPNRB frequentFlyer;
        public SabreFrequentFlyer(FrequentFlyerPNRB ff)
        {
            frequentFlyer = ff;
        }

        public string FrequentFlyerNo { get => frequentFlyer.Number; }
        public string CarrierCode { get => frequentFlyer.SupplierCode; }
        public List<string> PartnerCarrierCodes { get => frequentFlyer.PartnershipAirlineCodes?.ToList(); }
    }
}
