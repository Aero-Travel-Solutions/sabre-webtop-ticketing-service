using GetReservation;

namespace SabreWebtopTicketingService.Models
{
    internal class SabrePhoneNumber
    {
        private PhoneNumberPNRB phone;

        public SabrePhoneNumber(PhoneNumberPNRB p)
        {
            phone = p;
        }

        public string PhoneNumber => phone.Number;
        public string CityCode => phone.CityCode;
        public string Extension => phone.Extension;
        public string ID => phone.id;
    }
}