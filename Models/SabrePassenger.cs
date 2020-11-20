using GetReservation;
using SabreWebtopTicketingService.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Aeronology.Sabre.Test")]
namespace SabreWebtopTicketingService.Models
{
    internal class SabrePassenger
    {
        private PassengerPNRB pax;
        public SabrePassenger(PassengerPNRB passenger)
        {
            pax = passenger;
        }

        public string ReferenceNo => pax.nameAssocId;
        public string NameNumber => pax.nameId;
        public string ProfileID => pax.PassengerProfileID;
        public string Title => pax.Title;
        internal string FirstName => pax.FirstName;
        internal string LastName => pax.LastName;
        public string PassengerName => LastName + "/" + FirstName;
        public bool IsINF => pax.nameType == PassengerTypePNRB.I;
        public bool AccompanieByInfant => pax.withInfantSpecified ?
                                          pax.withInfant :
                                          pax.SpecialRequests != null &&
                                          pax.SpecialRequests.GenericSpecialRequest != null &&
                                          pax.SpecialRequests.GenericSpecialRequest.Where(s => s.Code == "INFT" && s.ActionCode == "KK").Count() >= 1;

        public string PaxType => Services.SabreSharedServices.GetPaxType(pax.passengerType, IsINF);

        public DateTime? DateOfBirth => GetDateOfBirth(pax);

        private DateTime? GetDateOfBirth(PassengerPNRB paxs)
        {
            DateTime? dob = null;


            dob = paxs.SpecialRequests != null &&
                  paxs.SpecialRequests.APISRequest != null &&
                  paxs.SpecialRequests.APISRequest.FirstOrDefault(w => w.DOCSEntry.DateOfBirthSpecified == true) != null ?
                  paxs.SpecialRequests.APISRequest.FirstOrDefault(w => w.DOCSEntry.DateOfBirthSpecified == true).DOCSEntry.DateOfBirth :
                  default(DateTime?);

            if (!dob.HasValue && PaxType == "CHD")
            {
                dob =   paxs.SpecialRequests != null &&
                        paxs.SpecialRequests.ChildRequest != null ?
                        paxs.SpecialRequests.ChildRequest.First().DateOfBirth != null ?
                            DateTime.Parse(paxs.SpecialRequests.ChildRequest.First().DateOfBirth) :
                            default(DateTime?) :
                        default(DateTime?);
            }

            return dob;
        }

        public string Gender => string.IsNullOrEmpty(pax.Gender) ?
                                    pax.SpecialRequests != null &&
                                    pax.SpecialRequests.APISRequest != null &&
                                    pax.SpecialRequests.APISRequest.FirstOrDefault(w => w.DOCSEntry.GenderSpecified) != null ?
                                        GetGender(pax.SpecialRequests.APISRequest.FirstOrDefault(w => w.DOCSEntry.GenderSpecified).DOCSEntry.Gender) :
                                        "":
                                    pax.Gender;

        private string GetGender(GenderDOCS_EntryPNRB gender)
        {
            switch (gender)
            {
                case GenderDOCS_EntryPNRB.M:
                    return "Male";
                case GenderDOCS_EntryPNRB.F:
                    return "Female";
                case GenderDOCS_EntryPNRB.MI:
                    return "Male";
                case GenderDOCS_EntryPNRB.FI:
                    return "Female";
                case GenderDOCS_EntryPNRB.U:
                    return "Not Specified";
                default:
                    return "";
            }
        }

        public string PhoneNumbers
        {
            get => pax.PhoneNumbers.
                            Select(s => s.Number +
                                        (string.IsNullOrEmpty(s.Extension) ?
                                                "" :
                                                " ext: " + s.Extension)
                                    ).FirstOrDefault();
        }
        public string EmailAddress { get => pax.EmailAddress.Select(s => s.Address).FirstOrDefault(); }
        public List<SabreFrequentFlyer> FrequentFlyer
        {
            get => pax.FrequentFlyer?.
                        Select(ff => new SabreFrequentFlyer(ff)).
                        ToList();
        }

        public List<SabreTicket> Tickets
        {
            get => pax.
                    TicketingInfo?.
                    TicketDetails?.
                    Select(tkt => new SabreTicket()
                    {
                        DocumentNumber = tkt.TicketNumber,
                        DocumentType = tkt.TransactionIndicator == "TE" ? "TKT" : "EMD",
                        RPH = int.Parse(tkt.index)
                    }).
                    ToList();
        }

        public List<SabreAncillary> AncillaryServices
        {
            get => pax.AncillaryServices == null ?
                        new List<SabreAncillary>() :
                        pax.AncillaryServices.
                        Select(s => new SabreAncillary(s, PassengerName)).
                        ToList();
        }

        public List<SabreFreeSeats> UnpaidSeats
        {
            get => pax.Seats?.PreReservedSeats == null ?
                        new List<SabreFreeSeats>() :
                        pax.Seats.PreReservedSeats.
                            Select(prs => new SabreFreeSeats()
                            {
                                SeatNumber = prs.SeatNumber,
                                NoSmoking = prs.SmokingPrefOfferedIndicatorSpecified && prs.SmokingPrefOfferedIndicator,
                                SeatType = prs.SeatTypeCode,
                                Status = prs.SeatStatusCode,
                                Origin = prs.BoardPoint,
                                Destination = prs.OffPoint,
                                NameNumber = prs.NameNumber,
                                Changed = prs.Changed                                
                            }).
                            ToList();
        }

        public SabreAPIS SecureFlightData
        {
            get => pax.SpecialRequests?.APISRequest == null ? 
                        null : 
                        new SabreAPIS(pax.SpecialRequests.APISRequest.FirstOrDefault()?.DOCSEntry);
        }

        public List<SabreSSR> SpecialRequests
        {
            get
            {
                List<SabreSSR> SSRs = new List<SabreSSR>();

                if (pax.SpecialRequests?.WheelchairRequest != null)
                {
                    SSRs.
                    AddRange(pax.SpecialRequests.WheelchairRequest.
                                 Select(wc => new SabreSSR()
                                 {
                                     SSRCode = wc.WheelchairCodeSpecified ? GetStringCode(wc.WheelchairCode) : "WCHR",
                                     Carrier = wc.VendorCode,
                                     Status = wc.ActionCode,
                                     From = wc.BoardCity,
                                     To = wc.OffCity,
                                     FlightNumber = wc.FlightNumber,
                                     BookingClass = wc.ClassOfService,
                                     DepartureDate = wc.FlightDate,
                                     NumberInParty = wc.NumberInParty,
                                     FreeText = wc.FreeText

                                 }).
                                 ToList());
                }

                if (pax.SpecialRequests?.SpecialMealRequest != null)
                {
                    SSRs.
                    AddRange(pax.SpecialRequests.SpecialMealRequest.
                                 Select(mc => new SabreSSR()
                                 {
                                     SSRCode = mc.MealType,
                                     Carrier = mc.VendorCode,
                                     Status = mc.ActionCode,
                                     From = mc.BoardCity,
                                     To = mc.OffCity,
                                     FlightNumber = mc.FlightNumber,
                                     BookingClass = mc.ClassOfService,
                                     DepartureDate = mc.FlightDate,
                                     NumberInParty = mc.NumberInParty,
                                     FreeText = mc.Comment
                                 }).
                                 ToList());
                }

                if (pax.SpecialRequests?.GenericSpecialRequest != null)
                {
                    //MAAS MH NO1 KULMEL0129S28NOV/LIMITED ENGLIS
                    //BIKE MH NN1 MELKUL0148S20NOV
                    //BLND MH NN1 KULMEL0129S28NOV

                    SSRs.
                    AddRange((from gc in pax.SpecialRequests.GenericSpecialRequest
                              let flightsummaryline = string.IsNullOrEmpty(gc.FullText)? "" : gc.FullText.LastMatch(@"([A-Z]{6}\d{4}[A-Z]\d{2}[A-Z]{3})")
                              select new SabreSSR()
                             {
                                 SSRCode = gc.Code,
                                 Carrier = gc.AirlineCode,
                                 Status = gc.ActionCode,
                                 FreeText = gc.FreeText,
                                 From = string.IsNullOrEmpty(flightsummaryline) ? "" : gc.FullText.LastMatch(@"([A-Z]{3})[A-Z]{3}\d{4}[A-Z]\d{2}[A-Z]{3}"),
                                 To = string.IsNullOrEmpty(flightsummaryline) ? "" : gc.FullText.LastMatch(@"[A-Z]{3}([A-Z]{3})\d{4}[A-Z]\d{2}[A-Z]{3}"),
                                 FlightNumber = string.IsNullOrEmpty(flightsummaryline) ? "" : gc.FullText.LastMatch(@"[A-Z]{6}(\d{4})[A-Z]\d{2}[A-Z]{3}"),
                                 BookingClass = string.IsNullOrEmpty(flightsummaryline) ? "" : gc.FullText.LastMatch(@"[A-Z]{6}\d{4}([A-Z])\d{2}[A-Z]{3}"),
                                 DepartureDate = string.IsNullOrEmpty(flightsummaryline) ? "" : gc.FullText.LastMatch(@"[A-Z]{6}\d{4}[A-Z](\d{2}[A-Z]{3})"),
                             }).
                            ToList());
                }

                if (pax.SpecialRequests?.UnaccompaniedMinorMessage != null)
                {
                    SSRs.
                    AddRange(pax.SpecialRequests.UnaccompaniedMinorMessage.
                                 Select(ur => new SabreSSR()
                                 {
                                     SSRCode = "UMNR",
                                     Carrier = ur.VendorCode,
                                     Status = ur.ActionCode,
                                     From = ur.BoardCity,
                                     To = ur.OffCity,
                                     FlightNumber = ur.FlightNumber,
                                     BookingClass = "",
                                     DepartureDate = ur.FlightDate,
                                     NumberInParty = "",
                                     FreeText = ""
                                 }).
                                 ToList());
                }

                if (pax.SpecialRequests?.SeatRequest != null)
                {
                    SSRs.
                    AddRange(pax.SpecialRequests.SeatRequest.
                                 Select(sr => new SabreSSR()
                                 {
                                     SSRCode = sr.SeatCode,
                                     Carrier = sr.VendorCode,
                                     Status = sr.ActionCode,
                                     From = sr.BoardCity,
                                     To = sr.OffCity,
                                     FlightNumber = sr.FlightNumber,
                                     BookingClass = sr.ClassOfService,
                                     DepartureDate = sr.FlightDate,
                                     FreeText = sr.Comment,
                                     NumberInParty = "",
                                 }).
                                 ToList());
                }

                if(pax.Seats?.SeatSpecialRequests != null)
                {
                    SSRs.
                        AddRange(pax.Seats.SeatSpecialRequests.
                         Select(sr => new SabreSSR()
                         {
                             SSRCode = sr.SeatCode,
                             Carrier = sr.VendorCode,
                             Status = sr.ActionCode,
                             From = sr.BoardCity,
                             To = sr.OffCity,
                             FlightNumber = sr.FlightNumber,
                             BookingClass = sr.ClassOfService,
                             DepartureDate = sr.FlightDate,
                             FreeText = sr.Comment,
                             NumberInParty = ""
                         }).
                         ToList());
                }

                if (pax.SpecialRequests?.TicketingRequest != null)
                {
                    SSRs.
                        AddRange(pax.SpecialRequests.TicketingRequest.
                         Select(tr => new SabreSSR()
                         {
                             SSRCode = "TKNM",
                             Carrier = tr.ValidatingCarrier,
                             Status = tr.ActionCode,
                             From = tr.BoardPoint,
                             To = tr.OffPoint,
                             FlightNumber = "",
                             BookingClass = tr.ClassOfService,
                             DepartureDate = tr.DateOfTravelSpecified ? tr.DateOfTravel.GetISODateString() : "",
                             NumberInParty = tr.NumberInParty,
                             FreeText = $"{tr.TicketNumber}"
                         }).
                         ToList());
                }

                return SSRs;
            }
        }

        private string GetStringCode(WheelchairCodePNRB wheelchairCode)
        {
            switch (wheelchairCode)
            {
                case WheelchairCodePNRB.WCHR:
                    return "WCHR";
                case WheelchairCodePNRB.WCHS:
                    return "WCHS";
                case WheelchairCodePNRB.WCHC:
                    return "WCHC";
                case WheelchairCodePNRB.WCBD:
                    return "WCBD";
                case WheelchairCodePNRB.WCBW:
                    return "WCBW";
                case WheelchairCodePNRB.WCMP:
                    return "WCMP";
                case WheelchairCodePNRB.WCOB:
                    return "WCOB";
                default:
                    return "WCHR";
            }
        }

        public AssociatedINF AssociatedINF
        {
            get
            {
                AssociatedINF inf = null;
                if (pax.SpecialRequests != null &&
                   pax.SpecialRequests.GenericSpecialRequest != null)
                {
                    GenericSpecialRequestPNRB inft = pax.SpecialRequests.GenericSpecialRequest.FirstOrDefault(f => f.Code == "INFT");
                    if (inft != null)
                    {
                        inf = new AssociatedINF(inft);
                    }
                }

                return inf;
            }

            private set { }
        }
    }

    public class SabreSSR
    {
        public string SSRCode { get; set; }
        public string Carrier { get; internal set; }
        public string Status { get; internal set; }
        public string From { get; internal set; }
        public string To { get; internal set; }
        public string FlightNumber { get; internal set; }
        public string BookingClass { get; internal set; }
        public string DepartureDate { get; internal set; }
        public string NumberInParty { get; internal set; }
        public string FreeText { get; internal set; }
    }

    public class SabreFreeSeats
    {
        public string SeatNumber { get; set; }
        public bool NoSmoking { get; set; }
        public string SeatType { get; set; }
        public string Status { get; set; }
        public string Origin {get;set;}
        public string Destination { get; set; }
        public string Changed { get; set; }
        public string NameNumber { get; set; }
    }

    public class SabreAPIS
    {
        private DOCSEntryPNRB apis;

        public SabreAPIS(DOCSEntryPNRB p)
        {
            apis = p;
        }

        public string Forename => apis?.Forename;
        public string MiddleName => apis?.MiddleName;
        public string Surname => apis?.Surname;
        public string Gender => apis!= null && apis.GenderSpecified ? apis.Gender.ToString() : "";
        public DateTime? DateOfBirth => apis != null && apis.DateOfBirthSpecified ? apis.DateOfBirth : default(DateTime?);
        public string VendorCode => apis?.VendorCode;
        public string DocumentNumber => apis?.DocumentNumber;
        public string CountryOfIssue => apis?.CountryOfIssue;
        public string DocumentNationalityCountry => apis?.DocumentNationalityCountry;
        public DateTime? DocumentExpirationDate => apis != null && apis.DocumentExpirationDateSpecified ? apis.DocumentExpirationDate : default(DateTime?);
        public string DocumentType => apis?.DocumentType;
        public bool PrimaryHolder => apis != null && apis.PrimaryHolderSpecified ? apis.PrimaryHolder : false;
    }

    internal class AssociatedINF
    {
        //SMITH/BARRY MSTR/03SEP18
        //SMITH/BARRY MSTR/03SEP2018
        internal GenericSpecialRequestPNRB inft;
        internal string[] items;
        public AssociatedINF(GenericSpecialRequestPNRB specialrequests)
        {
            inft = specialrequests;
            items = inft.FreeText.SplitOnRegex(@"/(.*)/(\d{1,2}[A-Za-z]{3}\d{2,4})").ToArray();
        }

        public string INFName => items[1];
        public string INFDOB => items[2];
    }
}
