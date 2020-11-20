using GetReservation;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{
    internal class SabreAncillary
    {
        private AncillaryServicesPNRB ancillary;

        public SabreAncillary(AncillaryServicesPNRB ancillaryServices, string  name)
        {
            ancillary = ancillaryServices;
            AssociatedPassengerName = name;
        }

        public string GroupKey => SabreSharedServices.GenerateGroupKey(ancillary.RficCode,
                                                   ancillary.RficSubcode,
                                                   ancillary.CommercialName + (RFIC == "A" ? string.Format(" ({0})", ancillary.PdcSeat) : ""),
                                                   ancillary.GroupCode);
        public int EMDNumber=> int.Parse(ancillary.sequenceNumber);
        public string ID => ancillary.id;
        public string EMDType => ancillary.EMDType; 
        public string RFIC => ancillary.RficCode;
        public string RFISC => ancillary.RficSubcode;
        public string GroupCode => ancillary.GroupCode;
        public string SSRCode => ancillary.SSRCode;
        public string CommercialName => ancillary.CommercialName + (RFIC == "A" ? string.Format(" ({0})", ancillary.PdcSeat) : "");
        public string SeatNumber => RFIC == "A" ? ancillary.PdcSeat : "";
        public string ApplicablePassengerTypeCode => string.IsNullOrEmpty(ancillary.PassengerTypeCode) ? "ADT" : ancillary.PassengerTypeCode;
        public string AssociatedPassengerName;
        public string Carrier => ancillary.OwningCarrierCode;
        public string Origin => ancillary.BoardPoint;
        public string Destination => ancillary.OffPoint;
        public string Endorsements  => ancillary.Endorsements; 
        public string TourCode=> ancillary.TourCode; 
        internal string BagWeightValue => ancillary.BagWeight?.Value;
        internal string BagWeightUnit => ancillary.BagWeight?.Unit;
        public string BagWeight => BagWeightValue + BagWeightUnit;
        internal string FareGuaranteedIndicator => ancillary.FareGuaranteedIndicator;
        public bool Guaranteed => FareGuaranteedIndicator == "T";
        public decimal BasePrice => ancillary.TotalTTLPrice.Price - (ancillary.Taxes?.Sum(tax => tax.TaxAmount) ?? 0.00M);
        public decimal TotalPrice => ancillary.TotalTTLPrice.Price;
        public string CurrencyCode => ancillary.TotalTTLPrice.Currency;
        public List<SabreAncillaryTax> Taxes => ancillary.Taxes?.Select(t => new SabreAncillaryTax(t)).ToList();
        public string Status => ancillary.ActionCode;
        public bool PurchaseByDateSpecified => ancillary.PurchaseByDateSpecified;
        public DateTime? PurchaseByDate => ancillary.PurchaseByDateSpecified ? 
                                            ancillary.PurchaseByDate:
                                            ancillary.PurchaseTimestampSpecified ?
                                                ancillary.PurchaseTimestamp:
                                                default(DateTime?);
        public List<Segment> AssociatedSector => GetAssociatedSectors(ancillary.Item1);

        private List<Segment> GetAssociatedSectors(object item1)
        {
            List<Segment> segments = new List<Segment>();
            SegmentOrTravelPortionType seg = item1 as SegmentOrTravelPortionType;

            if(seg != null)
            {
                segments.Add(
                    new Segment()
                    {
                        SectorNo = int.Parse(seg.sequence),
                        FlightNo = seg.FlightNumber,
                        DepartureDate = seg.DepartureDateSpecified ? seg.DepartureDate.GetISODateString() : DateTime.MinValue.GetISODateString(),
                        From = seg.BoardPoint,
                        To = seg.OffPoint,
                        BookingClass = seg.ClassOfService,
                        MarketingCarrier = seg.MarketingCarrier,
                        OperatingCarrier = seg.OperatingCarrier
                    });
                return segments;
            }

            AncillaryServicesPNRBTravelPortions travelportion = item1 as AncillaryServicesPNRBTravelPortions;
            if (travelportion != null)
            {
                segments.AddRange(travelportion.
                                        TravelPortion.
                                        Select(tp => new Segment
                                        {
                                            SectorNo = int.Parse(tp.sequence),
                                            FlightNo = tp.FlightNumber,
                                            DepartureDate = tp.DepartureDateSpecified ? tp.DepartureDate.GetISODateString() : DateTime.MinValue.GetISODateString(),
                                            From = tp.BoardPoint,
                                            To = tp.OffPoint,
                                            BookingClass = tp.ClassOfService,
                                            MarketingCarrier = tp.MarketingCarrier,
                                            OperatingCarrier = tp.OperatingCarrier
                                        }).
                                        ToList());
                return segments;
            }
            return segments;
        }
    }

    public class Segment
    {
        public int SectorNo { get; set; }
        public string FlightNo { get; set; }
        public string DepartureDate { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string BookingClass { get; set; }
        public string MarketingCarrier { get; set; }
        public string OperatingCarrier { get; set; }
    }
}
