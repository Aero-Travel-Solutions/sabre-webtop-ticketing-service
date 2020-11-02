using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
    public class TicketingPcc
  {
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("gds_code")]
    public string GdsCode { get; set; }

    [JsonPropertyName("pcc_code")]
    public string PccCode { get; set; }

    [JsonPropertyName("consolidator_id")]
    public string ConsolidatorId { get; set; }

    [JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; }

    [JsonPropertyName("created_date")]
    public string CreatedDate { get; set; }

    [JsonPropertyName("modified_by")]
    public string ModifiedBy { get; set; }

    [JsonPropertyName("modified_date")]
    public string ModifiedDate { get; set; }
  }
}
