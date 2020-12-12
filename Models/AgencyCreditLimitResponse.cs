namespace SabreWebtopTicketingService.Models
{
    public class AgencyCreditLimitResponse
    {
        public AgencyCreditLimit AgencyCreditLimit { get; set; }
        public bool CreditLimitCheckRequired { get; set; }
        public Error Error { get; set; }
    }
}
