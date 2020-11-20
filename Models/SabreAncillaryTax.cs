using GetReservation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    internal class SabreAncillaryTax
    {
        private AncillaryTaxPNRB ancillaryTax;
        public SabreAncillaryTax(AncillaryTaxPNRB taxPNRB)
        {
            ancillaryTax = taxPNRB;
        }
        public string TaxCode { get => ancillaryTax.TaxCode; }
        public decimal TaxAmount { get => ancillaryTax.TaxAmount; }
    }
}
