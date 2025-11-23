using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public int? MessageId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string? Content { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Message? Message { get; set; }

    public virtual User User { get; set; } = null!;
}
