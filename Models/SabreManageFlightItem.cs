using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TripSearch;

namespace SabreWebtopTicketingService.Models
{
    public class SabreManageFlightItem
    {
        GetReservationRS res;
        private static List<AirlineTTL> airlineTTLs = new List<AirlineTTL>();
        public SabreManageFlightItem(GetReservationRS response)
        {
            res = response;
            airlineTTLs = GetAirlineTTLs();
        }

        internal ReservationPNRB pnr => (ReservationPNRB)res.Item;

        public string Locator => pnr.BookingDetails.RecordLocator;
        public string BookedPCC => pnr.POS.Source.PseudoCityCode;
        public string PCCCityCode => pnr.PhoneNumbers.First().CityCode;

        public List<string> PassengerNames => pnr.
                                                PassengerReservation.
                                                Passengers.
                                                Passenger.
                                                Select(pax => string.Format("{0}/{1}", pax.LastName, pax.FirstName)).
                                                ToList();

        public List<ManageFlightSector> sectors
        {
            get
            {
                List<ManageFlightSector> secs = new List<ManageFlightSector>();

                if (pnr.PassengerReservation.Segments != null && pnr.PassengerReservation.Segments.Air != null)
                {
                    secs.
                        AddRange
                        (
                            pnr.
                                PassengerReservation.
                                Segments.
                                Air.
                                Select(s => new ManageFlightSector()
                                {
                                    SectorNo = s.sequence,
                                    Origin = s.DepartureAirport,
                                    Destination = s.ArrivalAirport,
                                    Carrier = s.MarketingAirlineCode,
                                    FlightNo = s.FlightNumber,
                                    BookingClass= s.ClassOfService,
                                    DepartureDate = DateTime.Parse(s.DepartureDateTime).GetISODateTime(),
                                    Status = s.ActionCode,
                                    MarriageGroup = s.MarriageGrp.Group == "0" ? "" : s.MarriageGrp.Group,
                                    Ticketed = s.SegmentSpecialRequests?.GenericSpecialRequest?.Where(w => !string.IsNullOrEmpty(w.TicketNumber))?.Any()??false
                                }).
                                ToList()
                        ); 
                }

                if (pnr.PassengerReservation.Segments != null && pnr.PassengerReservation.Segments.Arunk != null)
                {
                    secs.
                        AddRange
                        (
                            pnr.
                                PassengerReservation.
                                Segments.
                                Air.
                                Select(s => new ManageFlightSector()
                                {
                                    SectorNo = s.sequence,
                                    Origin = "ARUNK",
                                    Ticketed = s.SegmentSpecialRequests?.GenericSpecialRequest?.Where(w => string.IsNullOrEmpty(w.TicketNumber))?.Any() ?? false
                                }).
                                ToList()
                        );
                }

                return secs.OrderBy(o => o.SectorNo).ToList();
            }
        }

        public string CreatedDate => pnr.BookingDetails.CreationTimestamp.GetISODateTime();

        public string BookingTTL = airlineTTLs.IsNullOrEmpty() ? "" : airlineTTLs.FirstOrDefault(f => f.MostRestrictive)?.DisplayText;

        private List<AirlineTTL> GetAirlineTTLs()
        {
            DateTime? bookingcreateddate = pnr.BookingDetails.CreationTimestampSpecified ? pnr.BookingDetails.CreationTimestamp : default(DateTime?);
            List<string> applicablecarriers = pnr.PassengerReservation.Segments?.
                                                    Air?.
                                                    SelectMany(s => new[] { s.MarketingAirlineCode, s.OperatingAirlineCode }).
                                                    Distinct().
                                                    ToList();

            if ((applicablecarriers.IsNullOrEmpty())) { return new List<AirlineTTL>(); }

            List<string> adtklines = pnr.
                                        GenericSpecialRequests?.
                                        Where(w => !string.IsNullOrEmpty(w.FullText) &&
                                                   (w.FullText.StartsWith("ADTK") ||
                                                    w.FullText.StartsWith("ADMD") ||
                                                    w.FullText.StartsWith("OTHS")))?.
                                        Select(s => s.FullText).
                                        ToList();

            if ((adtklines.IsNullOrEmpty())) { return new List<AirlineTTL>(); }

            return SabreSharedServices.GetAirlineTTLs(adtklines, bookingcreateddate, applicablecarriers);
        }
    }
}
