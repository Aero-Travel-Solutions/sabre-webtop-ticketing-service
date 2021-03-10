using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class QueueModel
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("hash_key")]
        public string HashKey { get; set; }

        [JsonPropertyName("sort_key")]
        public string SortKey { get; set; }

        [JsonPropertyName("queue_id")]
        public string QueueID { get; set; }

        [JsonPropertyName("consolidator_id")]
        public string ConsolidatorId { get; set; }

        [JsonPropertyName("queue_date")]
        public string QueueDate { get; set; }

        [JsonPropertyName("queue_time")]
        public string QueueTime { get; set; }

        [JsonPropertyName("queue_type")]
        public string QueueType { get; set; }

        [JsonPropertyName("agency_name")]
        public string AgencyName { get; set; }

        [JsonPropertyName("agency_number")]
        public string AgencyNumber { get; set; }

        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; }

        [JsonPropertyName("record_locator")]
        public string RecordLocator { get; set; }

        [JsonPropertyName("gds_source")]
        public string GDSSource { get; set; }

        [JsonPropertyName("plating_carrier")]
        public string PlatingCarrier { get; set; }

        [JsonPropertyName("itineraries")]
        public List<Itinerary> Itineraries { get; set; }

        [JsonPropertyName("flight_type")]
        public string FlightType { get; set; }

        [JsonPropertyName("departure_date")]
        public string DepartureDate { get; set; }

        [JsonPropertyName("brief_description")]
        public string BriefDescription { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("sectors")]
        public List<string> Sectors { get; set; }

        [JsonPropertyName("request")]
        public string Request { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("queued_by")]
        public string QueuedBy { get; set; }

        [JsonPropertyName("downloaded_by")]
        public string DownloadedBy { get; set; }

        [JsonPropertyName("locked_by")]
        public string LockedBy { get; set; }

        [JsonPropertyName("completed_by")]
        public string CompletedBy { get; set; }

        [JsonPropertyName("archived_by")]
        public string ArchivedBy { get; set; }

        [JsonPropertyName("email_address")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("client_reference")]
        public string ClientReference { get; set; }

        [JsonPropertyName("fare_reference")]
        public string FareReference { get; set; }

        [JsonPropertyName("agent_queueback_no")]
        public string AgentQueueBackNo { get; set; }

        [JsonPropertyName("consolidator_ticketing_queue_no")]
        public string ConsolidatorTicketingQueueNo { get; set; }

        [JsonPropertyName("consolidator_ticketing_pcc")]
        public string ConsolidatorTicketingPCC { get; set; }

        [JsonPropertyName("agent_queueback_pcc")]
        public string AgentQueueBackPCC { get; set; }

        [JsonPropertyName("agent_additional_notes")]
        public string AgentAdditionalNotes { get; set; }

        [JsonPropertyName("reason_for_queueing")]
        public string ReasonForQueueing { get; set; }

        [JsonPropertyName("ticketing_error_message")]
        public string TicketingErrorMessage { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("passengers")]
        public List<QueuePassengers> Passengers { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }

        [JsonPropertyName("comments")]
        public List<Comment> Comments { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("acknowledgement_message")]
        public string AcknowledgementMessage { get; set; }

        [JsonPropertyName("prefatory_instruction_code")]
        public string PrefatoryInstructionCode { get; set; }

        [JsonPropertyName("consolidator_ticketing_pcc_username")]
        public string ConsolidatorTicketingPccUsername { get; set; }

        [JsonPropertyName("consolidator_ticketing_pcc_target_branch")]
        public string ConsolidatorTicketingTargetBranch { get; set; }

        [JsonPropertyName("account_login_used")]
        public string AccountLoginUsed { get; set; }

        public string warmer { get; set; }
    }

    public enum Status
    {
        Available,
        Completed,
        Downloaded,
        Locked
    }
}
