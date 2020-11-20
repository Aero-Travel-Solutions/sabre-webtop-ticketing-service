using GetReservation;
using SabreWebtopTicketingService.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Aeronology.Sabre.Test")]
namespace SabreWebtopTicketingService.Models
{
    public class SabreAirSector
    {
        private readonly SegmentTypePNRBSegmentAir airsec;
        private readonly short seqNo;
        public SabreAirSector(short seqNo, SegmentTypePNRBSegmentAir segmentTypePNRBAir)
        {
            airsec = segmentTypePNRBAir;
            this.seqNo = seqNo;
        }


        public SabreCabin Cabin => new SabreCabin(airsec.Cabin);

        public string Status => airsec.ActionCode;
        public short SequenceNo => seqNo;
        public string DepartureAirport => airsec.DepartureAirport;
        public string DepartureTerminalCode => airsec.DepartureTerminalCode;
        public string DepartureTerminalName => airsec.DepartureTerminalName;
        public DateTime DepartureDateTime => DateTime.Parse(airsec.DepartureDateTime);
        public string ArrivalAirport => airsec.ArrivalAirport;
        public string ArrivalTerminalCode => airsec.ArrivalTerminalCode;
        public string ArrivalTerminalName => airsec.ArrivalTerminalName;
        public DateTime ArrivalDateTime
        {
            get
            {
                DateTime temparrival = DateTime.Parse(airsec.ArrivalDateTime);
                if (temparrival == null)
                {
                    temparrival = DateTime.Parse(airsec.DepartureDateTime);
                }
                return temparrival;
            }

            private set { }
        }
        public string FlightNumber => airsec.FlightNumber;
        public string ClassOfService => airsec.ClassOfService;
        public bool CodeShare => airsec.CodeShareSpecified ? airsec.CodeShare : false;
        public string MarketingAirlineCode => airsec.MarketingAirlineCode;
        public string OperatingAirlineCode => airsec.OperatingAirlineCode;
        public string OperatingAirlineShortName => airsec.OperatingAirlineShortName;
        public string OperatingClassOfService => airsec.OperatingClassOfService;
        public string OperatingFlightNumber => airsec.OperatingFlightNumber;
        public string ElapsedTime => airsec.ElapsedTime;
        public string EquipmentType => airsec.EquipmentType;
        public decimal Mileage => decimal.Parse(airsec.AirMilesFlown ?? "0.00");
        public string MarriageGrp => airsec.MarriageGrp.Group == "0" ? "" : airsec.MarriageGrp.Group;
        public List<string> MealCodes => airsec.Meal.Select(meal => meal.Code).ToList();
        public List<SabreHiddenStop> HiddenStops => airsec.HiddenStop.Select(stop => new SabreHiddenStop(stop)).ToList();
        public string AirlineRecordLocator => string.IsNullOrEmpty(airsec.AirlineRefId) ? "" : airsec.AirlineRefId.Split("*", StringSplitOptions.RemoveEmptyEntries).Last();
        public List<SectorSSR> SegmentSSRs
        {
            get
            {
                List<SectorSSR> ssrs = new List<SectorSSR>();

                if (airsec.SegmentSpecialRequests?.GenericSpecialRequest != null)
                {
                    ssrs.
                        AddRange(airsec.
                                    SegmentSpecialRequests.
                                    GenericSpecialRequest.
                                    Select(s => new SectorGenericSSR(s)).
                                    Select(s => new SectorSSR()
                                    {
                                        SSRCode = s.SSRCode,
                                        Carrier = s.Carrier,
                                        FreeText = s.FreeText,
                                        NumberInParty = s.NumberInParty,
                                        Status = s.Status
                                    }).
                                    ToList());
                }

                if (airsec.SegmentSpecialRequests?.WheelchairRequest != null)
                {
                    ssrs.
                    AddRange(airsec.
                        SegmentSpecialRequests.
                        WheelchairRequest.
                        Select(s => new SectorWCHRSSR(s)).
                        Select(s => new SectorSSR()
                        {
                            SSRCode = s.SSRCode,
                            Carrier = s.Carrier,
                            FreeText = s.FreeText,
                            NumberInParty = s.NumberInParty,
                            Status = s.Status
                        }).
                        ToList());
                }

                ssrs.
                    Where(w => !string.IsNullOrEmpty(w.FreeText)).
                    ToList().
                    ForEach(f => f.FreeText = f.FreeText.TrimStart(new char[] { '/', ',' }));

                return ssrs;
            }
        }

        public bool Ticketed => airsec.SegmentSpecialRequests?.GenericSpecialRequest?.Where(w => !string.IsNullOrEmpty(w.TicketNumber))?.Any() ?? false;
    }

    public class SectorSSR
    {
        public string SSRCode { get; set; }
        public string Status { get; set; }
        public string NumberInParty { get; set; }
        public string Carrier { get; set; }
        public string FreeText { get; set; }
    }

    internal class SectorGenericSSR
    {
        /*
         * <stl19:GenericSpecialRequest id="345" type="G" msgType="S">
                                        <stl19:Code>AVML</stl19:Code>
                                        <stl19:FreeText>.NO MEALS SNACKSERVICE ONLY</stl19:FreeText>
                                        <stl19:ActionCode>NO</stl19:ActionCode>
                                        <stl19:NumberInParty>1</stl19:NumberInParty>
                                        <stl19:AirlineCode>LH</stl19:AirlineCode>
                                        <stl19:FullText>AVML LH NO1 MUCCDG2230L25OCT.NO MEALS SNACKSERVICE ONLY</stl19:FullText>
            </stl19:GenericSpecialRequest>
         */

        GenericSpecialRequestPNRB genericPNRB = null;
        public SectorGenericSSR(GenericSpecialRequestPNRB s)
        {
            genericPNRB = s;
        }

        public string SSRCode => genericPNRB.Code;
        public string Status => genericPNRB.ActionCode;
        public string NumberInParty => genericPNRB.NumberInParty;
        public string Carrier => genericPNRB.AirlineCode;
        public string FreeText => string.IsNullOrEmpty(genericPNRB.FreeText) ? "" : genericPNRB.FreeText.ReplaceAll(new String[] { ",", "." }, "").Trim();
    }

    internal class SectorWCHRSSR
    {
        /*
         * <stl19:GenericSpecialRequest id="345" type="G" msgType="S">
                                        <stl19:Code>AVML</stl19:Code>
                                        <stl19:FreeText>.NO MEALS SNACKSERVICE ONLY</stl19:FreeText>
                                        <stl19:ActionCode>NO</stl19:ActionCode>
                                        <stl19:NumberInParty>1</stl19:NumberInParty>
                                        <stl19:AirlineCode>LH</stl19:AirlineCode>
                                        <stl19:FullText>AVML LH NO1 MUCCDG2230L25OCT.NO MEALS SNACKSERVICE ONLY</stl19:FullText>
            </stl19:GenericSpecialRequest>
         */

        WheelchairRequestPNRB WCHRPNRB = null;
        public SectorWCHRSSR(WheelchairRequestPNRB s)
        {
            WCHRPNRB = s;
        }

        public string SSRCode => WCHRPNRB.WheelchairCodeSpecified ? GetWheelchaircode(WCHRPNRB.WheelchairCode) : "WCHR";

        private string GetWheelchaircode(WheelchairCodePNRB wheelchairCode)
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

        public string Status => WCHRPNRB.ActionCode;
        public string NumberInParty => WCHRPNRB.NumberInParty;
        public string Carrier => WCHRPNRB.VendorCode;
        public string FreeText => string.IsNullOrEmpty(WCHRPNRB.FreeText) ? "" : WCHRPNRB.FreeText.ReplaceAll(new String[] { ",", "." }, "").Trim();
    }
}
