using System.Text.Json.Serialization;

namespace DMP.BL.Models;

public class CfTurnstileResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error-codes")]
    public string[] ErrorCodes { get; set; } = null!;

    [JsonPropertyName("challenge_ts")]
    public string TimeStamp { get; set; } = null!;

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = null!;
}
