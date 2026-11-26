using System.Text.Json.Serialization;

namespace ClientAPP;

// Một tin chat server phát về qua event "chat_message"
public class ChatMessageModel
{

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("sent_at")]
    public DateTime SentAt { get; set; }
}