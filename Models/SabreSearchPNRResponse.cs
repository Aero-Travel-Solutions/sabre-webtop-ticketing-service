using SabreWebtopTicketingService.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TripSearch;

namespace SabreWebtopTicketingService.Models
{
    public class SabreSearchPNRResponse
    {
        public readonly string Locator = "";
        GetReservationRS res;
        public SabreSearchPNRResponse(string locator, GetReservationRS response)
        {
            Locator = locator;
            res = response;
        }

        internal ReservationPNRB pnr => (ReservationPNRB)res.Item;

        public List<string> PassengerNames => pnr.
                                                PassengerReservation.
                                                Passengers.
                                                Passenger.
                                                Select(pax => string.Format("{0}/{1}", pax.LastName, pax.FirstName)).
                                                ToList();

        public DateTime FirstDepartureDate => DateTime.Parse(pnr.
                                                PassengerReservation.
                                                Segments.
                                                Air.
                                                First().
                                                DepartureDateTime);
        public string FirstDepartureDateString => FirstDepartureDate.ToString("ddMMM");

        public string LastArrivalDate => DateTime.Parse(pnr.
                                            PassengerReservation.
                                            Segments.
                                            Air.
                                            Last().ArrivalDateTime).ToString("ddMMM");

        public string CreatedDate => pnr.BookingDetails.CreationTimestamp.GetISODateTime();

        public string BookingTTL = "";
    }
}
