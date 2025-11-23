using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class Conversation
{
    public int ConversationId { get; set; }

    public string? ConversationName { get; set; }

    public bool? IsGroup { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
