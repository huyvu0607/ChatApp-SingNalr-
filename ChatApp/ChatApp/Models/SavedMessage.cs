using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class SavedMessage
{
    public int SavedId { get; set; }

    public int UserId { get; set; }

    public int MessageId { get; set; }

    public string? Note { get; set; }

    public DateTime? SavedAt { get; set; }

    public virtual Message Message { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
