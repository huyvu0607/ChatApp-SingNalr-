using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class FriendRequest
{
    public int RequestId { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public string? Status { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
