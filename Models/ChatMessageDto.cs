using System;

namespace SCLogMate.Models;

public class ChatMessageDto
{
    public long Id { get; set; }
    public string Timestamp { get; set; } = "";
    public string? SessionId { get; set; }
    public string Channel { get; set; } = "Global"; // "Global", "Party", "Direct", "Channel"
    public string Sender { get; set; } = "";
    public string? Recipient { get; set; }
    public string Message { get; set; } = "";
    public string? RawOcr { get; set; }
    public bool IsFlagged { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
