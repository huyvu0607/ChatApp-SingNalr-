using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Models;

public partial class ChatAppContext : DbContext
{
    public ChatAppContext()
    {
    }

    public ChatAppContext(DbContextOptions<ChatAppContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<ConversationMember> ConversationMembers { get; set; }

    public virtual DbSet<Friend> Friends { get; set; }

    public virtual DbSet<FriendRequest> FriendRequests { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MessageEditHistory> MessageEditHistories { get; set; }

    public virtual DbSet<MessageReaction> MessageReactions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<SavedMessage> SavedMessages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-I957D0S;Database=ChatAppDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__Conversa__C050D877713713A1");

            entity.Property(e => e.ConversationName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsGroup).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Conversat__Creat__4D94879B");
        });

        modelBuilder.Entity<ConversationMember>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("PK__Conversa__0CF04B18299E9DC4");

            entity.HasIndex(e => e.ConversationId, "IX_ConversationMembers_ConversationId");

            entity.HasIndex(e => e.DeletedAt, "IX_ConversationMembers_DeletedAt");

            entity.HasIndex(e => e.IsArchived, "IX_ConversationMembers_IsArchived");

            entity.HasIndex(e => e.IsPinned, "IX_ConversationMembers_IsPinned");

            entity.HasIndex(e => e.UserId, "IX_ConversationMembers_UserId");

            entity.HasIndex(e => new { e.ConversationId, e.UserId }, "UC_ConversationMember").IsUnique();

            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.IsAdmin).HasDefaultValue(false);
            entity.Property(e => e.IsArchived).HasDefaultValue(false);
            entity.Property(e => e.IsPinned).HasDefaultValue(false);
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LastReadAt).HasColumnType("datetime");

            entity.HasOne(d => d.Conversation).WithMany(p => p.ConversationMembers)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("FK__Conversat__Conve__534D60F1");

            entity.HasOne(d => d.User).WithMany(p => p.ConversationMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Conversat__UserI__5441852A");
        });

        modelBuilder.Entity<Friend>(entity =>
        {
            entity.HasKey(e => e.FriendshipId).HasName("PK__Friends__4D531A545162E1AF");

            entity.HasIndex(e => e.FriendId, "IX_Friends_FriendId");

            entity.HasIndex(e => e.UserId, "IX_Friends_UserId");

            entity.HasIndex(e => new { e.UserId, e.FriendId }, "UC_Friendship").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.FriendNavigation).WithMany(p => p.FriendFriendNavigations)
                .HasForeignKey(d => d.FriendId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Friends__FriendI__403A8C7D");

            entity.HasOne(d => d.User).WithMany(p => p.FriendUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Friends__UserId__3F466844");
        });

        modelBuilder.Entity<FriendRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__FriendRe__33A8517A6C2C19E8");

            entity.HasIndex(e => e.ReceiverId, "IX_FriendRequests_ReceiverId");

            entity.HasIndex(e => e.SenderId, "IX_FriendRequests_SenderId");

            entity.HasIndex(e => e.Status, "IX_FriendRequests_Status");

            entity.HasIndex(e => new { e.SenderId, e.ReceiverId }, "UC_FriendRequest").IsUnique();

            entity.Property(e => e.RespondedAt).HasColumnType("datetime");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");

            entity.HasOne(d => d.Receiver).WithMany(p => p.FriendRequestReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FriendReq__Recei__46E78A0C");

            entity.HasOne(d => d.Sender).WithMany(p => p.FriendRequestSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FriendReq__Sende__45F365D3");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Messages__C87C0C9C17D0F968");

            entity.HasIndex(e => e.ConversationId, "IX_Messages_ConversationId");

            entity.HasIndex(e => e.IsDeleted, "IX_Messages_IsDeleted");

            entity.HasIndex(e => e.IsPinned, "IX_Messages_IsPinned");

            entity.HasIndex(e => e.SenderId, "IX_Messages_SenderId");

            entity.HasIndex(e => e.SentAt, "IX_Messages_SentAt").IsDescending();

            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.EditedAt).HasColumnType("datetime");
            entity.Property(e => e.FileUrl).HasMaxLength(255);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsEdited).HasDefaultValue(false);
            entity.Property(e => e.IsPinned).HasDefaultValue(false);
            entity.Property(e => e.MessageType)
                .HasMaxLength(20)
                .HasDefaultValue("text");
            entity.Property(e => e.PinnedAt).HasColumnType("datetime");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Conversation).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("FK__Messages__Conver__5AEE82B9");

            entity.HasOne(d => d.DeletedByNavigation).WithMany(p => p.MessageDeletedByNavigations)
                .HasForeignKey(d => d.DeletedBy)
                .HasConstraintName("FK__Messages__Delete__60A75C0F");

            entity.HasOne(d => d.PinnedByNavigation).WithMany(p => p.MessagePinnedByNavigations)
                .HasForeignKey(d => d.PinnedBy)
                .HasConstraintName("FK__Messages__Pinned__628FA481");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Messages__Sender__5BE2A6F2");
        });

        modelBuilder.Entity<MessageEditHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__MessageE__4D7B4ABD25BC2D76");

            entity.ToTable("MessageEditHistory");

            entity.HasIndex(e => e.MessageId, "IX_MessageEditHistory_MessageId");

            entity.Property(e => e.EditedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Message).WithMany(p => p.MessageEditHistories)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("FK__MessageEd__Messa__656C112C");
        });

        modelBuilder.Entity<MessageReaction>(entity =>
        {
            entity.HasKey(e => e.ReactionId).HasName("PK__MessageR__46DDF9B4FBB8B43C");

            entity.HasIndex(e => new { e.MessageId, e.UserId }, "UC_MessageReaction").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReactionType).HasMaxLength(20);

            entity.HasOne(d => d.Message).WithMany(p => p.MessageReactions)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("FK__MessageRe__Messa__6FE99F9F");

            entity.HasOne(d => d.User).WithMany(p => p.MessageReactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MessageRe__UserI__70DDC3D8");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E122C684BB8");

            entity.HasIndex(e => e.UserId, "IX_Notifications_UserId");

            entity.Property(e => e.Content).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.NotificationType).HasMaxLength(50);

            entity.HasOne(d => d.Message).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("FK__Notificat__Messa__75A278F5");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__74AE54BC");
        });

        modelBuilder.Entity<SavedMessage>(entity =>
        {
            entity.HasKey(e => e.SavedId).HasName("PK__SavedMes__0B058FDCBD4A65A5");

            entity.HasIndex(e => e.UserId, "IX_SavedMessages_UserId");

            entity.HasIndex(e => new { e.UserId, e.MessageId }, "UC_SavedMessage").IsUnique();

            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.SavedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Message).WithMany(p => p.SavedMessages)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SavedMess__Messa__6B24EA82");

            entity.HasOne(d => d.User).WithMany(p => p.SavedMessages)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SavedMess__UserI__6A30C649");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CB24D989A");

            entity.HasIndex(e => e.IsOnline, "IX_Users_IsOnline");

            entity.HasIndex(e => e.Username, "IX_Users_Username");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E429B95FAA").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534867D6D40").IsUnique();

            entity.Property(e => e.Avatar).HasMaxLength(255);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.CoverPhoto).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.IsOnline).HasDefaultValue(false);
            entity.Property(e => e.LastSeen).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
