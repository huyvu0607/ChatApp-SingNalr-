using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class ConversationMember
{
    public int MemberId { get; set; }

    public int ConversationId { get; set; }

    public int UserId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public DateTime? LastReadAt { get; set; }

    public bool? IsAdmin { get; set; }

    public bool? IsPinned { get; set; }

    public bool? IsArchived { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
