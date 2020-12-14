using FluentValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class BookSeatRequest
    {
        public string Locator { get; set; }
        public string BookedPCC { get; set; }
        public List<SeatData> BookSeats { get; set; }
        public List<SeatData> CancelSeats { get; set; }
    }

    public class SeatData
    {
        public string SeatNumber { get; set; }
        public string NameNumber { get; set; }
        public string SegmentNumber { get; set; }
    }


    //Fluent Validation
    public class BookSeatRequestValidator : AbstractValidator<BookSeatRequest>
    {
        public BookSeatRequestValidator()
        {
            RuleFor(x => x.Locator).NotNull().NotEmpty().Length(6).WithMessage("Locator not found or not in valid format").WithErrorCode("10000001");
        }
    }
}
