using System.Text.Json.Serialization;

namespace SabreWebtopTicketingService.Models
{
  public class AgentPcc
  {
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("gds_code")]
    public string GdsCode { get; set; }

    [JsonPropertyName("pcc_code")]
    public string PccCode { get; set; }

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; }

    [JsonPropertyName("opt_in")]
    public string[] OptIn { get; set; }

    [JsonPropertyName("consolidator_id")]
    public string ConsolidatorId { get; set; }

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

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
