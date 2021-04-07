using SabreWebtopTicketingService.CustomException;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SabreWebtopTicketingService.Common
{
    public static class CreditCardOperations
    {
        public static bool IsValidCreditCard(string cardNumber)
        {
            // check whether input string is null or empty 
            if (string.IsNullOrEmpty(cardNumber)) return false;

            //Remove unwanted characters found
            cardNumber = cardNumber.ReplaceAll(new string[] {" ", "  ", "   ",  "-", "_", " ", ":", "[", "]", "\t", "\r", "\n" });

            if (!cardNumber.IsMatch(@"^\d+$")) return false;

            //Luhn algorithm
            // 1. Starting with the check digit double the value of every other digit 
            // 2. If doubling of a number results in a two digits number, add up the digits to get a single digit number. This will results in eight single digit numbers 
            // 3. Get the sum of the digits
            // 4. Check the MOD 10 return 0
            return cardNumber.Where((e) => e >= '0' && e <= '9').Reverse().Select((e, i) => ((int)e - 48) * (i % 2 == 0 ? 1 : 2)).Sum((e) => e / 10 + e % 10) % 10 == 0;
        }

        public static string GetCreditCardType(string CreditCardNumber)
        {
            CreditCardNumber = CreditCardNumber.Trim();

            if (!IsValidCreditCard(CreditCardNumber))
            {
                throw new AeronologyException("50000011", string.Format("Invalid credit/debit card number {0}", CreditCardNumber.MaskNumber()));
            }

            Regex regVisa = new Regex("^4[0-9]{12}(?:[0-9]{3})?$");
            Regex regMaster = new Regex("^5[1-5][0-9]{14}$");
            Regex regExpress = new Regex("^3[47][0-9]{13}$");
            Regex regDiners = new Regex("^3(?:0[0-5]|[68][0-9])[0-9]{11}$");
            Regex regDiscover = new Regex("^6(?:011|5[0-9]{2})[0-9]{12}$");
            Regex regJCB = new Regex("^(?:2131|1800|35\\d{3})\\d{11}$");
            Regex regQantasAllocation = new Regex("^2081[0-9]{12}$");
            Regex regUATP = new Regex("^1|2[0-9]{12}(?:[0-9]{0,1,2,3})?$");


            if (regVisa.IsMatch(CreditCardNumber)) return "VI";
            if (regMaster.IsMatch(CreditCardNumber)) return "CA";
            if (regExpress.IsMatch(CreditCardNumber)) return "AX";
            if (regDiners.IsMatch(CreditCardNumber)) return "DN";
            if (regDiscover.IsMatch(CreditCardNumber)) return "DS";
            if (regJCB.IsMatch(CreditCardNumber)) return "JC";
            if (regQantasAllocation.IsMatch(CreditCardNumber)) return "TP";
            if (regUATP.IsMatch(CreditCardNumber)) return "TP";

            throw new AeronologyException("50000012", "Invalid credit/ debit card type");
        }

        public static bool IsValidExpiry(string expiryDate)
        {
            if (expiryDate.Length == 4)
            {
                expiryDate = $"{expiryDate.Substring(0, 2)}/{expiryDate.Substring(2, 2)}";
            }

            if (DateTime.TryParseExact(expiryDate, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expdate))
            {
                return expdate > DateTime.Today;
            }

            return false;            
        }

        public static List<StoredCreditCard> GetStoredCards(string text, bool ispricecommand = false)
        {

            if (string.IsNullOrEmpty(text)) 
            {
                return new List<StoredCreditCard>();
            }

            List<StoredCreditCard> res = new List<StoredCreditCard>();
            string pattern = ispricecommand ?
                                @"[A-Z]{2}((\d){13,16})[EXPIRYexpiry?\s_\-\/]*(\d{2}[-\/_~\s\\]*\d{2})" :
                                @"((?:\d[-\s_~]*?){13,16})[EXPIRYexpiry?\s_\-\/]*(\d{2}[-\/_~\s\\]*\d{2})";

            try
            {
                res = Regex
                        .Matches(text, pattern)
                        .Where(w=> w.Groups.Count >= 2 &&
                                    IsValidCreditCard(w.Groups[1].Value) &&
                                    IsValidExpiry(w.Groups[2].Value.ReplaceAll(new string[]{"-", "/", "_", "\\", "~", " ", "  " }, "/")))
                        .Select(m => new StoredCreditCard()
                        {
                            CreditCard = m.Groups[1].Value,
                            Expiry = m.Groups[2].Value,
                            MaskedCardNumber = m.Groups[1].Value.MaskNumber()
                        })
                        .ToList();
            }
            catch (Exception)
            {

            }

            return res;
        }
    }

    public class StoredCreditCard
    {
        public string CreditCard { get; set; }
        public string Expiry { get; set; }
        public string MaskedCardNumber { get; set; }
    }
}
