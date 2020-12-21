using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Services
{
    internal class SabreCurrencyConvert
    {
        //DC¥THB5000/AUD
        //RATE BSR 1THB - 0.04409316        AUD
        //AUD        220.46     TRUNCATED
        //AUD        221.00     ROUNDED UP TO NEXT   1      - FARES
        //AUD        220.50     ROUNDED UP TO NEXT   0.1    - TAXES
        private string res;

        public SabreCurrencyConvert(string res)
        {
            this.res = res;

            if(!res.Contains("RATE BSR"))
            {
                throw new GDSException("", res);
            }
        }
        private List<string> Lines => res.SplitOn("\n").ToList();

        public string CurrencyCode => Lines.First(f => f.Contains("ROUNDED UP TO NEXT   1")).Substring(0, 3);

        public int Amount => int.Parse(Lines.First(f => f.Contains("ROUNDED UP TO NEXT   1")).SplitOn("   ").Skip(1).First().Trim());
    }
}