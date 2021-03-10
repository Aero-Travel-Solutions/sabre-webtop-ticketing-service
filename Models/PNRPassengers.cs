using FluentValidation;
using System.Collections.Generic;
using System.ComponentModel;

namespace SabreWebtopTicketingService.Models
{
    public class PNRPassengers
    {
        public string NameNumber { get; set; }
        public string Title { get; set; }
        public string Passengername { get; set; }
        public string PaxType { get; set; }
        public string DOB { get; set; }
        public string Gender { get; set; }
        public FOP FOP { get; set; }
        public bool AccompaniedByInfant { get; set; }
        public bool SecureFlightDataExist { get; set; }
        public List<FrequentFlyer> FrequentFlyerDetails { get; set; }
        public string PassengerKey { get; set; }
    }

    public class FOP
    {
        public PaymentType PaymentType { get; set; }
        public string BCode { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string ApprovalCode { get; set; }
        public decimal CreditAmount { get; set; }
        public string CardType { get; set; }
        public string MaskedCardNumber { get; set; }
    }

    public class PNRPassengersValidator : AbstractValidator<PNRPassengers>
    {
        public PNRPassengersValidator()
        {
            RuleFor(x => x.Title).NotNull();
            RuleFor(x => x.Passengername).NotNull().Matches(@"[A-Z\/]*");
            RuleFor(x => x.PaxType).NotNull();
        }
    }
}