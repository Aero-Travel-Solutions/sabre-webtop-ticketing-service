using GetReservation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Models
{
    public class SabreCabin
    {
        private readonly AirTypeCabin cabin;

        public SabreCabin(AirTypeCabin airTypeCabin)
        {
            cabin = airTypeCabin;
        }

        public string CabinCode { get => cabin.Code; }
        public string CabinName { get => cabin.Name; }
        public string CabinShortName { get => cabin.ShortName; }
    }
}
