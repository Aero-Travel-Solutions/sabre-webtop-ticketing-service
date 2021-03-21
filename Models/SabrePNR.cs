using GetReservation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Services;

[assembly: InternalsVisibleTo("Aeronology.Sabre.Test")]
namespace SabreWebtopTicketingService.Models
{
    internal class SabrePNR
    {
        GetReservationRS getReservationRS;
        public SabrePNR(GetReservationRS rs)
        {
            getReservationRS = rs;
        }
        
        internal ReservationPNRB Reservation => ((ReservationPNRB)getReservationRS.Item);
           
        internal PriceQuoteXElement PriceQuote => Constants.xml == null ? null : new PriceQuoteXElement(Constants.xml);

        public string CreatedDate => Reservation.BookingDetails.CreationTimestamp.GetISODateTime();
        public string HostUserId => Reservation.BookingDetails.CreationAgentID.Last(2);
        public string DKNumber => Reservation.DKNumbers?.FirstOrDefault();
        public string Locator => Reservation.BookingDetails.RecordLocator;
        public string BookedPCC => Reservation.POS.Source.PseudoCityCode;
        public string AgentSine => Reservation.POS.Source.AgentSine;
        public string PCCCityCode => Reservation.PhoneNumbers.First().CityCode;
        public string FirstDeparturePoint => Reservation.POS.Source.FirstDepartPoint;
        public int NoOfPassengers => int.Parse(Reservation.numberInParty);
        public int NoOfInfats => Reservation.numberOfInfants;
        public int NoOfSegments => Reservation.NumberInSegment;
        public List<AirlineTTL> AirlineTTLs
        {
            get
            {
                DateTime? bookingcreateddate = Reservation.BookingDetails.CreationTimestampSpecified ? Reservation.BookingDetails.CreationTimestamp : default(DateTime?);
                List<string> applicablecarriers = Reservation.PassengerReservation.Segments?.
                                                        Air?.
                                                        SelectMany(s => new[] {s.MarketingAirlineCode, s.OperatingAirlineCode }).
                                                        Distinct().
                                                        ToList();

                if ((applicablecarriers.IsNullOrEmpty())) { return new List<AirlineTTL>(); }

                List<string> adtklines = Reservation.
                                            OpenReservationElements?.
                                            OpenReservationElement.
                                            Where(w =>
                                                w.Item.GetType() == typeof(ServiceRequestType) &&
                                                !string.IsNullOrEmpty(((ServiceRequestType)w.Item).code) &&
                                                "ADMD|ADTK|OTHS".Contains(((ServiceRequestType)w.Item).code)).
                                            Select(s => ((ServiceRequestType)s.Item).FullText).
                                            ToList();

                if ((adtklines.IsNullOrEmpty())) { return new List<AirlineTTL>(); }

                return Services.SabreSharedServices.GetAirlineTTLs(adtklines, bookingcreateddate, applicablecarriers);
            }
        }
        public List<SabrePassenger> Passengers => Reservation.PassengerReservation.Passengers.Passenger.Select(pax => new SabrePassenger(pax)).ToList();
        public List<SabreAirSector> AirSectors => (Reservation.PassengerReservation.Segments?.
                                                        Segment?.
                                                        Where(w => w.Item.GetType().Name == "SegmentTypePNRBSegmentAir").
                                                        Select(sec => new SabreAirSector(sec.sequence, (SegmentTypePNRBSegmentAir)sec.Item)).ToList()) ?? new List<SabreAirSector>();
        public List<SabreArunkSector> ArunkSectors => (Reservation.PassengerReservation.Segments?.
                                                        Segment?.
                                                        Where(w => w.Item.GetType().Name == "Arunk").
                                                        Select(sec => new SabreArunkSector(sec.sequence, (Arunk)sec.Item)).ToList()) ?? new List<SabreArunkSector>();
        public string ReceivedFrom => Reservation.ReceivedFrom.AgentName;
        public List<SabrePhoneNumber> PhoneNumbers => Reservation.PhoneNumbers.
                                                        Select(p => new SabrePhoneNumber(p)).ToList();

        public List<SabreOpenSSR> SSRs
        {
            get
            {
                List<SabreOpenSSR> ssrs = new List<SabreOpenSSR>();
                if (Reservation.OpenReservationElements?.OpenReservationElement != null)
                {
                    foreach (var ore in Reservation.
                                            OpenReservationElements.
                                            OpenReservationElement.
                                            Where(w => 
                                                w.Item.GetType() == typeof(ServiceRequestType) &&
                                                !string.IsNullOrEmpty(((ServiceRequestType)w.Item).code) &&
                                                !(("ADMD|ADTK|TKNE|DOCS|CTCM|CTCE|CTCR|INFT".Contains(((ServiceRequestType)w.Item).code)) ||
                                                ((ServiceRequestType)w.Item).serviceType == "OSI" ||
                                                string.IsNullOrEmpty(((ServiceRequestType)w.Item).actionCode))))
                    {
                        ServiceRequestType sr = (ServiceRequestType)ore.Item;

                        ssrs.
                            Add(new SabreOpenSSR()
                            {
                                SSRCode = sr.code,
                                Status = sr.actionCode,
                                Carrier = sr.airlineCode,
                                FreeText = string.Join(",", (new List<string>(){
                                                                    sr.FreeText?.TrimStart(new char[] { '/', '.', ',' }),
                                                                    sr.Comment?.TrimStart(new char[] { '/', '.', ',' })}).
                                                                    Where(w=> !string.IsNullOrEmpty(w)).
                                                                    Distinct()),
                                Sectors = ore.
                                            SegmentAssociation?.
                                            Where(w => w.Item != null).
                                            Select(s => new OpenSSRSector()
                                            {
                                                From = s.Item.BoardPoint,
                                                To = s.Item.OffPoint,
                                                Carrier = s.Item.CarrierCode ?? sr.airlineCode,
                                                BookingClass = s.Item.ClassOfService,
                                                FlightNumber = s.Item.FlightNumber,
                                                DepartureDate = s.Item.DepartureDate.GetISODateString()
                                            }).
                                            ToList(),
                                Passengers = ore.
                                                NameAssociation?.
                                                Select(s => new OpenSSRPassenger()
                                                {
                                                    FirstName = s.FirstName,
                                                    LastName = s.LastName,
                                                    NameNumber = s.ItemElementName == ItemChoiceType2.NameRefNumber ? ((string)s.Item).Replace("0", "") : ""
                                                }).
                                                ToList()
                            });
                    }
                }

                return ssrs;
            }
        }
        public List<StoredCreditCard> StoredCreditCard => getReservationRS == null ? 
                                                                new List<StoredCreditCard>():
                                                                new SabreStoredFOP((ReservationPNRB)getReservationRS.Item).storedCreditCards;

        public List<SabreTicket> Tickets => GetTickets(((ReservationPNRB)getReservationRS.Item).
                                                PassengerReservation.
                                                TicketingInfo.
                                                TicketDetails?.
                                                Where(w => w.TimestampSpecified).
                                                Select((t, index) => new SabreTicket()
                                                {
                                                    DocumentNumber = t.TicketNumber,
                                                    DocumentType = t.TransactionIndicator.StartsWith("T")? "TKT": "EMD",
                                                    TicketingPCC = t.AgencyLocation,
                                                    RPH = index,
                                                    PassengerName = t.PassengerName,
                                                    Voided = t.TransactionIndicator.IsMatch(@"^[TM]V$"),
                                                    IssueDate = t.Timestamp.GetISODateString(),
                                                    IssueTime = t.Timestamp.GetISOTimeString()
                                                }).
                                                ToList());

        private List<SabreTicket> GetTickets(List<SabreTicket> tkts)
        {
            List<SabreTicket> sabreTickets = new List<SabreTicket>();
            if (tkts==null)
            {
                return sabreTickets;
            }


            foreach (var tkt in tkts)
            {
                if(tkt.Voided)
                {
                    sabreTickets.First(f => f.DocumentNumber == tkt.DocumentNumber).Voided = true;
                    continue;
                }
                sabreTickets.Add(tkt);
            }
            return sabreTickets;
        }
    }
}
