using System;
using System.Collections.Generic;

namespace ChatApp.Models;

public partial class Friend
{
    public int FriendshipId { get; set; }

    public int UserId { get; set; }

    public int FriendId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User FriendNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
