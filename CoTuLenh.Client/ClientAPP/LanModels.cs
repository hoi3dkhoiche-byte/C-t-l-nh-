using System.Text.Json.Serialization;

namespace ClientAPP;

public sealed class MoveRequest
{
    [JsonPropertyName("piece_id")]
    public string PieceId { get; set; } = "";

    [JsonPropertyName("from_x")]
    public int FromX { get; set; }

    [JsonPropertyName("from_y")]
    public int FromY { get; set; }

    [JsonPropertyName("to_x")]
    public int ToX { get; set; }

    [JsonPropertyName("to_y")]
    public int ToY { get; set; }

    [JsonPropertyName("capture_mode")]
    public string CaptureMode { get; set; } = "occupy";

    [JsonPropertyName("move_stack")]
    public bool MoveStackTogether { get; set; } = true;
}
