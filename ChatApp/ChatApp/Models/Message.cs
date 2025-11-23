using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class Message
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public int SenderId { get; set; }

    public string? MessageText { get; set; }

    public string? MessageType { get; set; }

    public string? FileUrl { get; set; }

    public bool? IsPinned { get; set; }

    public bool? IsEdited { get; set; }

    public bool? IsDeleted { get; set; }

    public int? DeletedBy { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? PinnedAt { get; set; }

    public int? PinnedBy { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual User? DeletedByNavigation { get; set; }

    public virtual ICollection<MessageEditHistory> MessageEditHistories { get; set; } = new List<MessageEditHistory>();

    public virtual ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual User? PinnedByNavigation { get; set; }

    public virtual ICollection<SavedMessage> SavedMessages { get; set; } = new List<SavedMessage>();

    public virtual User Sender { get; set; } = null!;
}
