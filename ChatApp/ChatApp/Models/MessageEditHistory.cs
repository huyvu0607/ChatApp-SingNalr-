using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class MessageEditHistory
{
    public int HistoryId { get; set; }

    public int MessageId { get; set; }

    public string? OldMessageText { get; set; }

    public DateTime? EditedAt { get; set; }

    public virtual Message Message { get; set; } = null!;
}
