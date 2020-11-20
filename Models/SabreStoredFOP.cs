using GetReservation;
using SabreWebtopTicketingService.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SabreWebtopTicketingService.Models
{
    internal class SabreStoredFOP
    {
        private ReservationPNRB responseobj;
        private string response;

        public SabreStoredFOP(ReservationPNRB res)
        {
            responseobj = res;
            response = ObjectExtentions.ConvertObjectToXElement<ReservationPNRB>(res).ToString();
        }

        public List<StoredCreditCard> storedCreditCards
        {
            get
            {
                List<StoredCreditCard> result = new List<StoredCreditCard>();
                
                //1. CC stored in PNR body in remarks, etc
                result.AddRange(CreditCardOperations.GetStoredCards(response));

                //2. CC stored as FOP
                if (responseobj.OpenReservationElements?.OpenReservationElement != null)
                {                    
                    result.AddRange(responseobj.
                                        OpenReservationElements.
                                        OpenReservationElement.
                                        Select(s => s.Item).
                                        Where(w => w.GetType().Name == "FormOfPayment").
                                        Cast<GetReservation.FormOfPayment>().
                                        Select(s => s.Item).
                                        Where(w => w.GetType().Name == "PaymentCard").
                                        Cast<PaymentCard>().
                                        Where(w => w.PaymentType == "CC" &&
                                                   CreditCardOperations.IsValidCreditCard(w.CardNumber?.Value) &&
                                                   CreditCardOperations.IsValidExpiry(w.ExpiryMonth.Last(2) + "/" + w.ExpiryYear.Last(2))).
                                        Select(s => new StoredCreditCard()
                                        {
                                            CreditCard = s.CardNumber.Value,
                                            Expiry = s.ExpiryMonth.Last(2) + "/" + s.ExpiryYear.Last(2)
                                        }).
                                        ToList());
                }

                //3. Remove repeated cards
                result = result.GroupBy(g => g.CreditCard?.Trim()).Select(s => s.First()).ToList();

                //4. Mask the credit card number and generate an ID for cache
                result.ForEach(f =>
                {
                    f.MaskedCardNumber = f.CreditCard.Trim().MaskNumber();
                });

                return result;
            }
            private set { }
        }
    }
}