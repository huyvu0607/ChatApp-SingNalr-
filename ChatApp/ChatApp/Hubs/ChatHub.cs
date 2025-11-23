using ChatApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace ChatApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatAppContext _context;
        private static readonly ConcurrentDictionary<int, HashSet<string>> UserConnections = new();

        public ChatHub(ChatAppContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                UserConnections.AddOrUpdate(
                    userId,
                    _ => new HashSet<string> { Context.ConnectionId },
                    (_, set) =>
                    {
                        set.Add(Context.ConnectionId);
                        return set;
                    });

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = true;
                    user.LastSeen = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                await NotifyUserOnlineStatus(userId, true);

                var conversationIds = await _context.ConversationMembers
                    .Where(cm => cm.UserId == userId && cm.DeletedAt == null)
                    .Select(cm => cm.ConversationId)
                    .ToListAsync();

                foreach (var convId in conversationIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{convId}");
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                if (UserConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);

                    if (connections.Count == 0)
                    {
                        UserConnections.TryRemove(userId, out _);

                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            user.IsOnline = false;
                            user.LastSeen = DateTime.Now;
                            await _context.SaveChangesAsync();
                        }

                        await NotifyUserOnlineStatus(userId, false);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ========== SEND MESSAGE ==========
        public async Task SendMessage(int conversationId, string messageText)
        {
            var userId = GetCurrentUserId();

            // ✅ THÊM LOG để debug
            Console.WriteLine($"🔔 [SendMessage] Called by UserId={userId}, ConvId={conversationId}, Text='{messageText}'");

            if (userId <= 0)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId && cm.DeletedAt == null);
            if (!isMember)
            {
                await Clients.Caller.SendAsync("Error", "Không có quyền!");
                return;
            }
            if (string.IsNullOrWhiteSpace(messageText))
            {
                await Clients.Caller.SendAsync("Error", "Tin nhắn rỗng!");
                return;
            }

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                MessageText = messageText.Trim(),
                SentAt = DateTime.Now
            };

            _context.Messages.Add(message);
            var conv = await _context.Conversations.FindAsync(conversationId);
            if (conv != null) conv.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // ✅ THÊM LOG
            Console.WriteLine($"✅ [SendMessage] Saved MessageId={message.MessageId} to DB");

            var sender = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.UserId, u.Username, u.FullName, u.Avatar })
                .FirstOrDefaultAsync();

            var payload = new
            {
                messageId = message.MessageId,
                conversationId = conversationId,
                messageText = message.MessageText,
                sentAt = message.SentAt?.ToString("HH:mm"),
                isEdited = false,
                isPinned = false,
                sender = new
                {
                    userId = sender.UserId,
                    username = sender.Username,
                    fullName = sender.FullName,
                    avatar = sender.Avatar ?? "/images/default-avatar.png"
                },
                reactions = new List<object>()
            };

            await Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveMessage", payload);

            // ✅ THÊM LOG
            Console.WriteLine($"📡 [SendMessage] Broadcasted MessageId={message.MessageId} to conversation_{conversationId}");
        }

        // ========== REACT TO MESSAGE ==========
        public async Task ReactToMessage(int messageId, string reactionType)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return;

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return;

            var existing = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

            if (existing != null)
            {
                if (existing.ReactionType == reactionType)
                    _context.MessageReactions.Remove(existing);
                else
                {
                    existing.ReactionType = reactionType;
                    existing.CreatedAt = DateTime.Now;
                }
            }
            else
            {
                _context.MessageReactions.Add(new MessageReaction
                {
                    MessageId = messageId,
                    UserId = userId,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            var reactions = await _context.MessageReactions
                .Where(r => r.MessageId == messageId)
                .GroupBy(r => r.ReactionType)
                .Select(g => new
                {
                    reactionType = g.Key,
                    count = g.Count(),
                    userIds = g.Select(x => x.UserId).ToList()
                })
                .ToListAsync();

            await Clients.Group($"conversation_{message.ConversationId}")
                .SendAsync("ReactionUpdated", new
                {
                    messageId = messageId,
                    reactions = reactions
                });
        }

        // ========== EDIT MESSAGE ==========
        public async Task EditMessage(int messageId, string newText)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return;

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null || message.SenderId != userId)
            {
                await Clients.Caller.SendAsync("Error", "Không có quyền chỉnh sửa!");
                return;
            }

            var history = new MessageEditHistory
            {
                MessageId = messageId,
                OldMessageText = message.MessageText,
                EditedAt = DateTime.Now
            };
            _context.MessageEditHistories.Add(history);

            message.MessageText = newText.Trim();
            message.IsEdited = true;
            message.EditedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await Clients.Group($"conversation_{message.ConversationId}").SendAsync("MessageEdited", new
            {
                messageId = messageId,
                newText = message.MessageText,
                editedAt = message.EditedAt?.ToString("HH:mm")
            });
        }

        // ========== DELETE MESSAGE ==========
        public async Task DeleteMessage(int messageId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return;

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null || message.SenderId != userId)
            {
                await Clients.Caller.SendAsync("Error", "Không có quyền xóa!");
                return;
            }

            message.IsDeleted = true;
            message.DeletedAt = DateTime.Now;
            message.DeletedBy = userId;

            await _context.SaveChangesAsync();

            await Clients.Group($"conversation_{message.ConversationId}").SendAsync("MessageDeleted", new
            {
                messageId = messageId
            });
        }

        // ========== TYPING INDICATORS ==========
        public async Task UserTyping(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            await Clients.OthersInGroup($"conversation_{conversationId}").SendAsync("UserTyping", new
            {
                userId = userId,
                conversationId = conversationId,
                fullName = user.FullName ?? user.Username
            });
        }

        public async Task UserStoppedTyping(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return;

            await Clients.OthersInGroup($"conversation_{conversationId}").SendAsync("UserStoppedTyping", new
            {
                userId = userId,
                conversationId = conversationId
            });
        }

        // ========== MARK AS READ ==========
        public async Task MarkAsRead(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return;

            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

            if (member != null)
            {
                member.LastReadAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await Clients.Group($"conversation_{conversationId}").SendAsync("MessageRead", new
                {
                    conversationId = conversationId,
                    userId = userId,
                    lastReadAt = DateTime.Now
                });
            }
        }

        // ========== HELPER METHODS ==========
        private int GetCurrentUserId()
        {
            // ✅ CÁCH 1: Lấy từ Session (khớp với BaseController)
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                var userId = httpContext.Session.GetInt32("UserId");
                if (userId.HasValue && userId.Value > 0)
                {
                    Console.WriteLine($"✅ [GetCurrentUserId] Got UserId from Session: {userId.Value}");
                    return userId.Value;
                }
            }

            // ✅ CÁCH 2: Fallback - Lấy từ Claims (nếu có)
            var userIdClaim = Context.User?.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out var userIdFromClaim))
            {
                Console.WriteLine($"✅ [GetCurrentUserId] Got UserId from Claim: {userIdFromClaim}");
                return userIdFromClaim;
            }

            // ❌ Không tìm thấy UserId
            Console.WriteLine("❌ [GetCurrentUserId] Failed to get UserId from both Session and Claims");
            if (httpContext?.Session != null)
            {
                Console.WriteLine($"   Session IsAvailable: {httpContext.Session.IsAvailable}");
                Console.WriteLine($"   Session Keys: {string.Join(", ", httpContext.Session.Keys)}");
            }

            return 0;
        }

        private async Task NotifyUserOnlineStatus(int userId, bool isOnline)
        {
            var conversationIds = await _context.ConversationMembers
                .Where(cm => cm.UserId == userId && cm.DeletedAt == null)
                .Select(cm => cm.ConversationId)
                .ToListAsync();

            foreach (var convId in conversationIds)
            {
                await Clients.Group($"conversation_{convId}").SendAsync("UserStatusChanged", new
                {
                    userId = userId,
                    isOnline = isOnline,
                    lastSeen = DateTime.Now
                });
            }
        }
    }
}