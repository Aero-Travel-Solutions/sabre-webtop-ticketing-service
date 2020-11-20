using FluentValidation;

namespace SabreWebtopTicketingService.Models
{
    public class PNRStoredCards
    {
        public string MaskedCardNumber { get; set; }
        public string Expiry { get; set; }
    }

    //Fluent Validation
    public class PNRStoredCardsValidator : AbstractValidator<PNRStoredCards>
    {
        public PNRStoredCardsValidator()
        {
            RuleFor(x => x.MaskedCardNumber).NotNull().NotEmpty().WithMessage("Masked Display Text can't be empty").WithErrorCode("10000003");
            RuleFor(x => x.Expiry).NotNull().NotEmpty().WithMessage("Expiry can't be empty").WithErrorCode("10000010");
        }
    }
}