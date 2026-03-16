using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccountService.Models;

public class TransactionRequest
{
    [JsonPropertyName("operation")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionOperation? Operation { get; set; }

    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("reference_id")]
    public string ReferenceId { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; set; }
}
