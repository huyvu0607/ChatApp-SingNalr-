using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class MessageReaction
{
    public int ReactionId { get; set; }

    public int MessageId { get; set; }

    public int UserId { get; set; }

    public string ReactionType { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Message Message { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
