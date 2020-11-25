using System;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Common
{
    public class SabreSession
    {
        [JsonPropertyName("sabre_session_id")]
        public string SessionID { get; set; }
        public bool Stored { get; set; }
        public bool IsLimitReached { get; set; }
        public string AccessKey
        {
            get
            {
                string pcc = string.IsNullOrEmpty(CurrentPCC) ? ConsolidatorPCC : CurrentPCC;
                return ($"{pcc} - {Locator}").EncodeBase64();
            }
            private set { }
        }
        public string ConsolidatorPCC { get; set; }
        /// <summary>
        /// Set only if we emulate to an agent PCC
        /// </summary>
        public string CurrentPCC { get; set; }
        /// <summary>
        /// Set when you retrieve a PNR and remove when session is cleared by emulating to a different PCC
        /// </summary>
        public string Locator { get; set; }
        public DateTime CreatedDateTime { get; set; }
        /// <summary>
        /// indicate if the session is extracted from cache
        /// </summary>
        public bool StoredSesson { get; set; }
        /// <summary>
        /// auto populated property
        /// If lastmodified date is 15 or earlier we set expired to true
        /// </summary>
        public bool Expired => (DateTime.Now - CreatedDateTime).TotalMinutes > 14;
    }
}
