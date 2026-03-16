using System.Text.Json.Serialization;

namespace AccountService.Models;

public class TransactionResponse
{
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<TransactionStatus>))]
    public TransactionStatus Status { get; set; }

    [JsonPropertyName("balance")]
    public long Balance { get; set; }

    [JsonPropertyName("reserved_balance")]
    public long ReservedBalance { get; set; }

    [JsonPropertyName("available_balance")]
    public long AvailableBalance { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}
