using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatApp.Models;
using System.Linq;
using System.Reflection;
using System.Dynamic;
using ChatApp.Extensions;

namespace ChatApp.Controllers
{
    public class ChatController : BaseController
    {
        public ChatController(ChatAppContext context) : base(context)
        {
        }
        // GET: Chat/Index - Trang chủ chat
        public async Task<IActionResult> Index()
        {
            // ✅ DEBUG: Kiểm tra UserId từ Session
            Console.WriteLine("===========================================");
            Console.WriteLine($"🔍 [Chat/Index] CurrentUserId: {CurrentUserId}");
            Console.WriteLine($"   Username: {CurrentUsername}");
            Console.WriteLine($"   Session IsAvailable: {HttpContext.Session.IsAvailable}");

            if (CurrentUserId == 0)
            {
                Console.WriteLine("❌ [Chat/Index] UserId = 0! Not logged in!");
                return RedirectToAction("Login", "Auth");
            }

            // ✅ DEBUG: Query database trực tiếp để kiểm tra
            var memberCount = await _context.ConversationMembers
                .CountAsync(cm => cm.UserId == CurrentUserId && cm.DeletedAt == null);

            Console.WriteLine($"   Database: Found {memberCount} conversation memberships");

            var conversations = await GetUserConversationsAsync(CurrentUserId);

            Console.WriteLine($"   GetUserConversationsAsync returned: {conversations.Count} conversations");
            Console.WriteLine("===========================================");

            return View(conversations);
        }

        // GET: Chat/Conversation/{id} - Xem chi tiết conversation
        public async Task<IActionResult> Conversation(int id)
        {
            if (!await IsConversationMemberAsync(id, CurrentUserId))
            {
                ShowErrorMessage("Bạn không có quyền truy cập cuộc hội thoại này!");
                return RedirectToAction("Index");
            }

            var conversation = await _context.Conversations
                .Include(c => c.ConversationMembers)
                .ThenInclude(cm => cm.User)
                .FirstOrDefaultAsync(c => c.ConversationId == id);

            if (conversation == null)
            {
                ShowErrorMessage("Cuộc hội thoại không tồn tại!");
                return RedirectToAction("Index");
            }

            var messages = await GetConversationMessagesAsync(id, CurrentUserId);

            // Cập nhật thời gian đọc cuối
            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == id && cm.UserId == CurrentUserId);

            if (member != null)
            {
                member.LastReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // Dùng ViewBag hết → View không cần @model mạnh nữa
            ViewBag.Conversation = conversation;
            ViewBag.Messages = messages;
            ViewBag.IsAdmin = await IsConversationAdminAsync(id, CurrentUserId);
            ViewBag.CurrentUserId = CurrentUserId;

            // QUAN TRỌNG: Không truyền model vào View nữa!
            return View();
            // hoặc return View("Conversation"); nếu bạn đặt tên file khác default
        }

        // POST: Chat/SendMessage - Gửi tin nhắn
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SendMessage(int conversationId, string messageText, string messageType = "text", string fileUrl = null)
        //{
        //    if (!await IsConversationMemberAsync(conversationId, CurrentUserId))
        //    {
        //        return Json(new { success = false, message = "Bạn không có quyền gửi tin nhắn!" });
        //    }

        //    if (string.IsNullOrWhiteSpace(messageText) && string.IsNullOrWhiteSpace(fileUrl))
        //    {
        //        return Json(new { success = false, message = "Nội dung tin nhắn không được rỗng!" });
        //    }

        //    var message = new Message
        //    {
        //        ConversationId = conversationId,
        //        SenderId = CurrentUserId,
        //        MessageText = messageText?.Trim(),
        //        MessageType = messageType,
        //        FileUrl = fileUrl,
        //        SentAt = DateTime.Now,
        //        IsEdited = false,
        //        IsDeleted = false,
        //        IsPinned = false
        //    };

        //    _context.Messages.Add(message);

        //    // Cập nhật UpdatedAt của conversation
        //    var conversation = await _context.Conversations.FindAsync(conversationId);
        //    if (conversation != null)
        //    {
        //        conversation.UpdatedAt = DateTime.Now;
        //    }

        //    await _context.SaveChangesAsync();

        //    return Json(new
        //    {
        //        success = true,
        //        message = "Gửi tin nhắn thành công!",
        //        messageId = message.MessageId,
        //        sentAt = message.SentAt?.ToString("HH:mm")
        //    });
        //}

        // POST: Chat/EditMessage - Chỉnh sửa tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int messageId, string newText)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
            {
                return Json(new { success = false, message = "Tin nhắn không tồn tại!" });
            }

            if (message.SenderId != CurrentUserId)
            {
                return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa tin nhắn này!" });
            }

            if (string.IsNullOrWhiteSpace(newText))
            {
                return Json(new { success = false, message = "Nội dung tin nhắn không được rỗng!" });
            }

            // Lưu lịch sử chỉnh sửa
            var history = new MessageEditHistory
            {
                MessageId = messageId,
                OldMessageText = message.MessageText,
                EditedAt = DateTime.Now
            };
            _context.MessageEditHistories.Add(history);

            // Cập nhật tin nhắn
            message.MessageText = newText.Trim();
            message.IsEdited = true;
            message.EditedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Chỉnh sửa tin nhắn thành công!" });
        }

        // POST: Chat/DeleteMessage - Thu hồi tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
            {
                return Json(new { success = false, message = "Tin nhắn không tồn tại!" });
            }

            if (message.SenderId != CurrentUserId)
            {
                return Json(new { success = false, message = "Bạn không có quyền thu hồi tin nhắn này!" });
            }

            // Soft delete
            message.IsDeleted = true;
            message.DeletedAt = DateTime.Now;
            message.DeletedBy = CurrentUserId;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thu hồi tin nhắn thành công!" });
        }

        // POST: Chat/PinMessage - Ghim tin nhắn (chỉ admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PinMessage(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
            {
                return Json(new { success = false, message = "Tin nhắn không tồn tại!" });
            }

            if (!await IsConversationAdminAsync(message.ConversationId, CurrentUserId))
            {
                return Json(new { success = false, message = "Chỉ admin mới có thể ghim tin nhắn!" });
            }

            message.IsPinned = !(message.IsPinned ?? false);
            message.PinnedAt = message.IsPinned == true ? DateTime.Now : null;
            message.PinnedBy = message.IsPinned == true ? CurrentUserId : null;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isPinned = message.IsPinned,
                message = message.IsPinned == true ? "Đã ghim tin nhắn!" : "Đã bỏ ghim tin nhắn!"
            });
        }

        // POST: Chat/ReactMessage - Thả reaction cho tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactMessage(int messageId, string reactionType)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
            {
                return Json(new { success = false, message = "Tin nhắn không tồn tại!" });
            }

            // Kiểm tra đã react chưa
            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == CurrentUserId);

            if (existingReaction != null)
            {
                // Nếu react cùng loại -> xóa reaction
                if (existingReaction.ReactionType == reactionType)
                {
                    _context.MessageReactions.Remove(existingReaction);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Đã bỏ reaction!", removed = true });
                }
                else
                {
                    // Nếu react khác loại -> cập nhật
                    existingReaction.ReactionType = reactionType;
                    existingReaction.CreatedAt = DateTime.Now;
                }
            }
            else
            {
                // Thêm reaction mới
                var reaction = new MessageReaction
                {
                    MessageId = messageId,
                    UserId = CurrentUserId,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.Now
                };
                _context.MessageReactions.Add(reaction);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thả reaction!", removed = false });
        }

        // POST: Chat/SaveMessage - Lưu tin nhắn quan trọng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMessage(int messageId, string note = null)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
            {
                return Json(new { success = false, message = "Tin nhắn không tồn tại!" });
            }

            // Kiểm tra đã lưu chưa
            var existingSaved = await _context.SavedMessages
                .FirstOrDefaultAsync(sm => sm.MessageId == messageId && sm.UserId == CurrentUserId);

            if (existingSaved != null)
            {
                return Json(new { success = false, message = "Tin nhắn đã được lưu trước đó!" });
            }

            var savedMessage = new SavedMessage
            {
                MessageId = messageId,
                UserId = CurrentUserId,
                Note = note,
                SavedAt = DateTime.Now
            };

            _context.SavedMessages.Add(savedMessage);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã lưu tin nhắn!" });
        }

        // POST: Chat/UnsaveMessage - Bỏ lưu tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsaveMessage(int messageId)
        {
            var savedMessage = await _context.SavedMessages
                .FirstOrDefaultAsync(sm => sm.MessageId == messageId && sm.UserId == CurrentUserId);

            if (savedMessage == null)
            {
                return Json(new { success = false, message = "Tin nhắn chưa được lưu!" });
            }

            _context.SavedMessages.Remove(savedMessage);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã bỏ lưu tin nhắn!" });
        }

        // POST: Chat/PinConversation - Ghim hội thoại
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PinConversation(int conversationId)
        {
            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == CurrentUserId);

            if (member == null)
            {
                return Json(new { success = false, message = "Bạn không phải thành viên của cuộc hội thoại này!" });
            }

            member.IsPinned = !(member.IsPinned ?? false);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isPinned = member.IsPinned,
                message = member.IsPinned == true ? "Đã ghim hội thoại!" : "Đã bỏ ghim hội thoại!"
            });
        }

        // POST: Chat/ArchiveConversation - Lưu trữ hội thoại
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConversation(int conversationId)
        {
            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == CurrentUserId);

            if (member == null)
            {
                return Json(new { success = false, message = "Bạn không phải thành viên của cuộc hội thoại này!" });
            }

            member.IsArchived = !(member.IsArchived ?? false);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isArchived = member.IsArchived,
                message = member.IsArchived == true ? "Đã lưu trữ hội thoại!" : "Đã bỏ lưu trữ hội thoại!"
            });
        }

        // POST: Chat/DeleteConversation - Xóa hội thoại (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConversation(int conversationId)
        {
            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == CurrentUserId);

            if (member == null)
            {
                return Json(new { success = false, message = "Bạn không phải thành viên của cuộc hội thoại này!" });
            }

            member.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa hội thoại!" });
        }

        // POST: Chat/CreateGroup - Tạo nhóm chat
        // POST: Chat/CreateGroup - Tạo nhóm chat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(string groupName, List<int> memberIds)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return Json(new { success = false, message = "Tên nhóm không được rỗng!" });
            }

            if (memberIds == null || memberIds.Count == 0)
            {
                return Json(new { success = false, message = "Phải chọn ít nhất 1 thành viên!" });
            }

            // ✅ FIX: Loại bỏ creator nếu có trong memberIds để tránh trùng lặp
            var distinctMemberIds = memberIds.Where(id => id != CurrentUserId).Distinct().ToList();

            // ✅ FIX: Kiểm tra ít nhất 2 người (không bao gồm creator)
            // Tổng cộng sẽ có 3 người: creator + 2 members
            if (distinctMemberIds.Count < 2)
            {
                return Json(new
                {
                    success = false,
                    message = "Nhóm phải có ít nhất 3 người (bạn + 2 thành viên khác)!"
                });
            }

            // Tạo conversation mới
            var conversation = new Conversation
            {
                ConversationName = groupName.Trim(),
                IsGroup = true,
                CreatedBy = CurrentUserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Thêm creator là admin
            var creatorMember = new ConversationMember
            {
                ConversationId = conversation.ConversationId,
                UserId = CurrentUserId,
                JoinedAt = DateTime.Now,
                IsAdmin = true
            };
            _context.ConversationMembers.Add(creatorMember);

            // Thêm các thành viên khác (đã loại bỏ creator)
            foreach (var memberId in distinctMemberIds)
            {
                var member = new ConversationMember
                {
                    ConversationId = conversation.ConversationId,
                    UserId = memberId,
                    JoinedAt = DateTime.Now,
                    IsAdmin = false
                };
                _context.ConversationMembers.Add(member);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Tạo nhóm thành công!",
                conversationId = conversation.ConversationId
            });
        }


        // GET: Chat/GetFriendsForGroup - Lấy danh sách bạn bè để tạo nhóm
        public async Task<IActionResult> GetFriendsForGroup()
        {
            var friends = await _context.Friends
                .Where(f => f.UserId == CurrentUserId)
                .Include(f => f.FriendNavigation)
                .Select(f => f.FriendNavigation)
                .ToListAsync();

            return PartialView("_CreateGroupModal", friends);
        }

        // POST: Chat/UpdateConversationName - Đổi tên nhóm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateConversationName(int conversationId, string newName)
        {
            if (!await IsConversationAdminAsync(conversationId, CurrentUserId))
            {
                return Json(new { success = false, message = "Chỉ admin mới có thể đổi tên nhóm!" });
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                return Json(new { success = false, message = "Tên nhóm không được rỗng!" });
            }

            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation == null)
            {
                return Json(new { success = false, message = "Nhóm không tồn tại!" });
            }

            conversation.ConversationName = newName.Trim();
            conversation.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã đổi tên nhóm!" });
        }

        // POST: Chat/AddMember - Thêm thành viên vào nhóm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int conversationId, int userId)
        {
            if (!await IsConversationAdminAsync(conversationId, CurrentUserId))
            {
                return Json(new { success = false, message = "Chỉ admin mới có thể thêm thành viên!" });
            }

            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation == null || conversation.IsGroup != true)
            {
                return Json(new { success = false, message = "Nhóm không tồn tại!" });
            }

            // Kiểm tra đã là thành viên chưa
            var existingMember = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

            if (existingMember != null && existingMember.DeletedAt == null)
            {
                return Json(new { success = false, message = "Người này đã là thành viên!" });
            }

            if (existingMember != null && existingMember.DeletedAt != null)
            {
                // Khôi phục thành viên
                existingMember.DeletedAt = null;
                existingMember.JoinedAt = DateTime.Now;
            }
            else
            {
                // Thêm thành viên mới
                var newMember = new ConversationMember
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    JoinedAt = DateTime.Now,
                    IsAdmin = false
                };
                _context.ConversationMembers.Add(newMember);
            }

            conversation.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm thành viên!" });
        }

        // POST: Chat/RemoveMember - Xóa thành viên khỏi nhóm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int conversationId, int userId)
        {
            if (!await IsConversationAdminAsync(conversationId, CurrentUserId))
            {
                return Json(new { success = false, message = "Chỉ admin mới có thể xóa thành viên!" });
            }

            // Không cho phép xóa chính mình
            if (userId == CurrentUserId)
            {
                return Json(new { success = false, message = "Không thể tự xóa chính mình!" });
            }

            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

            if (member == null)
            {
                return Json(new { success = false, message = "Thành viên không tồn tại!" });
            }

            // Soft delete
            member.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa thành viên!" });
        }

        // POST: Chat/LeaveGroup - Rời nhóm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveGroup(int conversationId)
        {
            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == CurrentUserId);

            if (member == null)
            {
                return Json(new { success = false, message = "Bạn không phải thành viên của nhóm này!" });
            }

            var conversation = await _context.Conversations
                .Include(c => c.ConversationMembers)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation == null || conversation.IsGroup != true)
            {
                return Json(new { success = false, message = "Nhóm không tồn tại!" });
            }

            // Đếm số admin còn lại
            var adminCount = conversation.ConversationMembers
                .Count(m => m.IsAdmin == true && m.DeletedAt == null && m.UserId != CurrentUserId);

            if (member.IsAdmin == true && adminCount == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn là admin duy nhất! Hãy phong admin cho người khác trước khi rời nhóm."
                });
            }

            // Soft delete
            member.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã rời nhóm!" });
        }

        // POST: Chat/PromoteToAdmin - Phong admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(int conversationId, int userId)
        {
            if (!await IsConversationAdminAsync(conversationId, CurrentUserId))
            {
                return Json(new { success = false, message = "Chỉ admin mới có thể phong admin!" });
            }

            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

            if (member == null)
            {
                return Json(new { success = false, message = "Thành viên không tồn tại!" });
            }

            if (member.IsAdmin == true)
            {
                return Json(new { success = false, message = "Thành viên này đã là admin!" });
            }

            member.IsAdmin = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã phong admin!" });
        }

        // POST: Chat/DemoteFromAdmin - Hủy admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteFromAdmin(int conversationId, int userId)
        {
            if (!await IsConversationAdminAsync(conversationId, CurrentUserId))
            {
                return Json(new { success = false, message = "Chỉ admin mới có thể hủy admin!" });
            }

            // Không cho phép tự hủy admin của chính mình
            if (userId == CurrentUserId)
            {
                return Json(new { success = false, message = "Không thể tự hủy admin của chính mình!" });
            }

            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

            if (member == null)
            {
                return Json(new { success = false, message = "Thành viên không tồn tại!" });
            }

            if (member.IsAdmin != true)
            {
                return Json(new { success = false, message = "Thành viên này không phải admin!" });
            }

            member.IsAdmin = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy admin!" });
        }

        // GET: Chat/GetConversationInfo - Lấy thông tin chi tiết hội thoại
        public async Task<IActionResult> GetConversationInfo(int id)
        {
            if (!await IsConversationMemberAsync(id, CurrentUserId))
            {
                return Json(new { success = false, message = "Bạn không có quyền truy cập!" });
            }

            var conversation = await _context.Conversations
                .Include(c => c.ConversationMembers)
                .ThenInclude(cm => cm.User)
                .FirstOrDefaultAsync(c => c.ConversationId == id);

            if (conversation == null)
            {
                return Json(new { success = false, message = "Cuộc hội thoại không tồn tại!" });
            }

            var info = new
            {
                conversationId = conversation.ConversationId,
                conversationName = conversation.ConversationName,
                isGroup = conversation.IsGroup,
                createdAt = conversation.CreatedAt,
                members = conversation.ConversationMembers
                    .Where(m => m.DeletedAt == null)
                    .Select(m => new
                    {
                        userId = m.UserId,
                        username = m.User.Username,
                        fullName = m.User.FullName,
                        avatar = m.User.Avatar,
                        isAdmin = m.IsAdmin,
                        isOnline = m.User.IsOnline,
                        joinedAt = m.JoinedAt
                    }).ToList()
            };

            return Json(new { success = true, data = info });
        }

        // POST: Chat/MuteConversation - Tắt/Bật thông báo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MuteConversation(int conversationId, bool mute)
        {
            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == CurrentUserId);

            if (member == null)
            {
                return Json(new { success = false, message = "Bạn không phải thành viên!" });
            }

            // Note: Cần thêm trường IsMuted vào bảng ConversationMembers
            // member.IsMuted = mute;
            // await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = mute ? "Đã tắt thông báo!" : "Đã bật thông báo!",
                isMuted = mute
            });
        }

        // GET: Chat/SearchMessages - Tìm kiếm tin nhắn trong conversation
        public async Task<IActionResult> SearchMessages(int conversationId, string query)
        {
            if (!await IsConversationMemberAsync(conversationId, CurrentUserId))
            {
                return Json(new { success = false, message = "Bạn không có quyền truy cập!" });
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Vui lòng nhập từ khóa tìm kiếm!" });
            }

            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == CurrentUserId);

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId
                        && m.IsDeleted == false
                        && (member.DeletedAt == null || m.SentAt > member.DeletedAt) // ← SỬA Ở ĐÂY
                        && m.MessageText.Contains(query))
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .Select(m => new
                {
                    messageId = m.MessageId,
                    messageText = m.MessageText,
                    sentAt = m.SentAt,
                    sender = new
                    {
                        userId = m.Sender.UserId,
                        fullName = m.Sender.FullName,
                        username = m.Sender.Username
                    }
                })
                .ToListAsync();

            return Json(new { success = true, messages = messages });
        }

        // GET: Chat/GetSavedMessages - Lấy danh sách tin nhắn đã lưu
        public async Task<IActionResult> GetSavedMessages()
        {
            var savedMessages = await _context.SavedMessages
                .Where(sm => sm.UserId == CurrentUserId)
                .Include(sm => sm.Message)
                .ThenInclude(m => m.Sender)
                .Include(sm => sm.Message.Conversation)
                .OrderByDescending(sm => sm.SavedAt)
                .Select(sm => new
                {
                    savedId = sm.SavedId,
                    messageId = sm.MessageId,
                    messageText = sm.Message.MessageText,
                    note = sm.Note,
                    savedAt = sm.SavedAt,
                    sender = new
                    {
                        userId = sm.Message.Sender.UserId,
                        fullName = sm.Message.Sender.FullName,
                        avatar = sm.Message.Sender.Avatar
                    },
                    conversation = new
                    {
                        conversationId = sm.Message.ConversationId,
                        conversationName = sm.Message.Conversation.ConversationName,
                        isGroup = sm.Message.Conversation.IsGroup
                    }
                })
                .ToListAsync();

            return PartialView("_SavedMessages", savedMessages);
        }

        // Helper Methods
        private async Task<List<dynamic>> GetUserConversationsAsync(int userId)
        {
            try
            {
                Console.WriteLine($"📊 [GetUserConversationsAsync] Starting for userId={userId}");

                // ✅ BƯỚC 1: Lấy danh sách ConversationMembers trước
                var userConversations = await _context.ConversationMembers
                    .AsNoTracking()
                    .Where(cm => cm.UserId == userId && cm.DeletedAt == null)
                    .Include(cm => cm.Conversation)
                    .ToListAsync();

                Console.WriteLine($"   Found {userConversations.Count} conversations");

                if (userConversations.Count == 0)
                {
                    return new List<dynamic>();
                }

                var conversationIds = userConversations.Select(cm => cm.ConversationId).ToList();

                // ✅ BƯỚC 2: Lấy tất cả members của các conversations này
                var allMembers = await _context.ConversationMembers
                    .AsNoTracking()
                    .Where(cm => conversationIds.Contains(cm.ConversationId) && cm.DeletedAt == null)
                    .Include(cm => cm.User)
                    .ToListAsync();

                // ✅ BƯỚC 3: Lấy last message của từng conversation
                var lastMessages = await _context.Messages
                    .AsNoTracking()
                    .Where(m => conversationIds.Contains(m.ConversationId)
                             && (m.IsDeleted == null || m.IsDeleted == false))
                    .GroupBy(m => m.ConversationId)
                    .Select(g => g.OrderByDescending(m => m.SentAt ?? DateTime.MinValue).FirstOrDefault())
                    .ToListAsync();

                // ✅ BƯỚC 4: Xây dựng kết quả
                var result = new List<dynamic>();

                foreach (var cm in userConversations)
                {
                    var conversation = cm.Conversation;

                    // Get members của conversation này
                    var conversationMembers = allMembers
                        .Where(m => m.ConversationId == conversation.ConversationId)
                        .Select(m => new
                        {
                            UserId = m.UserId,
                            Username = m.User?.Username ?? "Unknown",
                            FullName = m.User?.FullName ?? "Unknown",
                            Avatar = !string.IsNullOrEmpty(m.User?.Avatar)
                                ? m.User.Avatar
                                : "/images/default-avatar.png",
                            IsOnline = m.User?.IsOnline ?? false,
                            IsDeleted = m.DeletedAt != null
                        })
                        .ToList();

                    // Get last message
                    var lastMsg = lastMessages.FirstOrDefault(m => m?.ConversationId == conversation.ConversationId);

                    // Đếm unread messages - ✅ FIX: Tránh lỗi conversion
                    int unreadCount = 0;
                    try
                    {
                        var lastReadAt = cm.LastReadAt ?? DateTime.MinValue;
                        var deletedAt = cm.DeletedAt;

                        unreadCount = await _context.Messages
                            .Where(m => m.ConversationId == conversation.ConversationId
                                     && m.SenderId != userId
                                     && (m.IsDeleted == null || m.IsDeleted == false)
                                     && (m.SentAt != null && m.SentAt > lastReadAt)
                                     && (deletedAt == null || (m.SentAt != null && m.SentAt > deletedAt)))
                            .CountAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ⚠️ Error counting unread for ConvId={conversation.ConversationId}: {ex.Message}");
                    }

                    // Tạo dynamic object
                    dynamic conv = new ExpandoObject();
                    conv.ConversationId = conversation.ConversationId;
                    conv.ConversationName = conversation.ConversationName ?? "";
                    conv.IsGroup = conversation.IsGroup ?? false;
                    conv.IsPinned = cm.IsPinned ?? false;
                    conv.IsArchived = cm.IsArchived ?? false;
                    conv.LastReadAt = cm.LastReadAt;
                    conv.UpdatedAt = conversation.UpdatedAt ?? DateTime.MinValue;
                    conv.Members = conversationMembers.Cast<object>().ToList();
                    conv.LastMessage = lastMsg != null ? new
                    {
                        MessageText = lastMsg.MessageText ?? "",
                        SentAt = lastMsg.SentAt ?? DateTime.MinValue,
                        SenderId = lastMsg.SenderId
                    } : null;
                    conv.UnreadCount = unreadCount;

                    result.Add(conv);
                    Console.WriteLine($"   ✅ Added ConvId={conversation.ConversationId}, Unread={unreadCount}");
                }

                // ✅ Sắp xếp: Pinned trước, sau đó theo UpdatedAt
                result = result
                    .OrderByDescending(c => ((dynamic)c).IsPinned)
                    .ThenByDescending(c => ((dynamic)c).UpdatedAt)
                    .ToList();

                Console.WriteLine($"✅ [GetUserConversationsAsync] Completed with {result.Count} conversations");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [GetUserConversationsAsync] Error: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                return new List<dynamic>();
            }
        }
        private async Task<List<dynamic>> GetConversationMessagesAsync(int conversationId, int userId, int pageSize = 50)
        {
            try
            {
                var member = await _context.ConversationMembers
                    .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

                var messages = await _context.Messages
                    .Where(m => m.ConversationId == conversationId
                            && (m.IsDeleted == null || m.IsDeleted == false)
                            && (member.DeletedAt == null || (m.SentAt != null && m.SentAt > member.DeletedAt)))
                    .Include(m => m.Sender)
                    .Include(m => m.MessageReactions)
                    .OrderByDescending(m => m.SentAt ?? DateTime.MinValue)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        m.MessageId,
                        MessageText = m.MessageText ?? "",
                        MessageType = m.MessageType ?? "text",
                        m.FileUrl,
                        IsPinned = m.IsPinned ?? false,
                        IsEdited = m.IsEdited ?? false,
                        SentAt = m.SentAt ?? DateTime.MinValue,
                        EditedAt = m.EditedAt,
                        Sender = new
                        {
                            m.Sender.UserId,
                            Username = m.Sender.Username ?? "",
                            FullName = m.Sender.FullName ?? "",
                            Avatar = m.Sender.Avatar ?? "/images/default-avatar.png"
                        },
                        Reactions = m.MessageReactions.Select(mr => new
                        {
                            mr.UserId,
                            ReactionType = mr.ReactionType ?? ""
                        }).ToList(),
                        IsSaved = _context.SavedMessages.Any(sm => sm.MessageId == m.MessageId && sm.UserId == userId)
                    })
                    .ToListAsync();

                messages.Reverse();
                return messages.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [GetConversationMessagesAsync] Error: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                return new List<dynamic>();
            }
        }
        // Thêm các methods này vào ChatController.cs

        // GET: Chat/SearchUsers - Tìm kiếm người dùng để kết bạn
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Vui lòng nhập từ khóa tìm kiếm!" });
            }

            query = query.Trim().ToLower();

            // Tìm users theo Email, Username, FullName, PhoneNumber
            var users = await _context.Users
                .Where(u => u.UserId != CurrentUserId // Không tìm chính mình
                        && (u.Email.ToLower().Contains(query)
                            || u.Username.ToLower().Contains(query)
                            || u.FullName.ToLower().Contains(query)
                            || (u.PhoneNumber != null && u.PhoneNumber.Contains(query))))
                .Select(u => new
                {
                    userId = u.UserId,
                    username = u.Username,
                    email = u.Email,
                    fullName = u.FullName,
                    avatar = u.Avatar,
                    bio = u.Bio,
                    isOnline = u.IsOnline
                })
                .Take(20) // Giới hạn 20 kết quả
                .ToListAsync();

            if (!users.Any())
            {
                return Json(new { success = true, users = new List<object>(), message = "Không tìm thấy người dùng nào!" });
            }

            // Kiểm tra relationship với từng user
            var userIds = users.Select(u => u.userId).ToList();

            // Lấy danh sách bạn bè
            var friendIds = await _context.Friends
                .Where(f => f.UserId == CurrentUserId && userIds.Contains(f.FriendId))
                .Select(f => f.FriendId)
                .ToListAsync();

            // Lấy danh sách friend requests đã gửi
            var sentRequestIds = await _context.FriendRequests
                .Where(fr => fr.SenderId == CurrentUserId
                          && userIds.Contains(fr.ReceiverId)
                          && fr.Status == "pending")
                .Select(fr => fr.ReceiverId)
                .ToListAsync();

            // Lấy danh sách friend requests đã nhận
            var receivedRequestIds = await _context.FriendRequests
                .Where(fr => fr.ReceiverId == CurrentUserId
                          && userIds.Contains(fr.SenderId)
                          && fr.Status == "pending")
                .Select(fr => fr.SenderId)
                .ToListAsync();

            // Gắn relationship status vào từng user
            var result = users.Select(u => new
            {
                u.userId,
                u.username,
                u.email,
                u.fullName,
                u.avatar,
                u.bio,
                u.isOnline,
                relationshipStatus = GetRelationshipStatus(u.userId, friendIds, sentRequestIds, receivedRequestIds)
            }).ToList();

            return Json(new { success = true, users = result });
        }

        // Helper: Xác định relationship status
        private string GetRelationshipStatus(int userId, List<int> friendIds, List<int> sentRequestIds, List<int> receivedRequestIds)
        {
            if (friendIds.Contains(userId))
                return "friend"; // Đã là bạn bè

            if (sentRequestIds.Contains(userId))
                return "request_sent"; // Đã gửi lời mời kết bạn

            if (receivedRequestIds.Contains(userId))
                return "request_received"; // Đã nhận lời mời kết bạn

            return "none"; // Chưa có quan hệ - có thể kết bạn
        }

        // POST: Chat/SendFriendRequest - Gửi lời mời kết bạn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendFriendRequest(int receiverId)
        {
            if (receiverId == CurrentUserId)
            {
                return Json(new { success = false, message = "Không thể kết bạn với chính mình!" });
            }

            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại!" });
            }

            // Kiểm tra đã là bạn bè chưa
            var isFriend = await _context.Friends
                .AnyAsync(f => f.UserId == CurrentUserId && f.FriendId == receiverId);

            if (isFriend)
            {
                return Json(new { success = false, message = "Các bạn đã là bạn bè!" });
            }

            // Kiểm tra đã gửi request chưa
            var existingRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(fr => fr.SenderId == CurrentUserId
                                        && fr.ReceiverId == receiverId
                                        && fr.Status == "pending");

            if (existingRequest != null)
            {
                return Json(new { success = false, message = "Bạn đã gửi lời mời kết bạn trước đó!" });
            }

            // Kiểm tra có request từ người kia chưa (nếu có thì tự động accept)
            var reverseRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(fr => fr.SenderId == receiverId
                                        && fr.ReceiverId == CurrentUserId
                                        && fr.Status == "pending");

            if (reverseRequest != null)
            {
                // Tự động accept và tạo friendship 2 chiều
                reverseRequest.Status = "accepted";
                reverseRequest.RespondedAt = DateTime.Now;

                var friendship1 = new Friend
                {
                    UserId = CurrentUserId,
                    FriendId = receiverId,
                    CreatedAt = DateTime.Now
                };

                var friendship2 = new Friend
                {
                    UserId = receiverId,
                    FriendId = CurrentUserId,
                    CreatedAt = DateTime.Now
                };

                _context.Friends.AddRange(friendship1, friendship2);

                // Tạo conversation 1-1
                var conversation = new Conversation
                {
                    IsGroup = false,
                    CreatedBy = CurrentUserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                var member1 = new ConversationMember
                {
                    ConversationId = conversation.ConversationId,
                    UserId = CurrentUserId,
                    JoinedAt = DateTime.Now
                };

                var member2 = new ConversationMember
                {
                    ConversationId = conversation.ConversationId,
                    UserId = receiverId,
                    JoinedAt = DateTime.Now
                };

                _context.ConversationMembers.AddRange(member1, member2);

                // Tạo notification
                var notification = new Notification
                {
                    UserId = receiverId,
                    NotificationType = "friend_accepted",
                    Content = $"{CurrentUserFullName} đã chấp nhận lời mời kết bạn của bạn",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã kết bạn thành công!",
                    relationshipStatus = "friend"
                });
            }

            // Tạo friend request mới
            var friendRequest = new FriendRequest
            {
                SenderId = CurrentUserId,
                ReceiverId = receiverId,
                Status = "pending",
                SentAt = DateTime.Now
            };

            _context.FriendRequests.Add(friendRequest);

            // Tạo notification cho receiver
            var notif = new Notification
            {
                UserId = receiverId,
                NotificationType = "friend_request",
                Content = $"{CurrentUserFullName} đã gửi lời mời kết bạn",
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã gửi lời mời kết bạn!",
                relationshipStatus = "request_sent"
            });
        }

        // POST: Chat/CancelFriendRequest - Hủy lời mời kết bạn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelFriendRequest(int receiverId)
        {
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(fr => fr.SenderId == CurrentUserId
                                        && fr.ReceiverId == receiverId
                                        && fr.Status == "pending");

            if (request == null)
            {
                return Json(new { success = false, message = "Không tìm thấy lời mời kết bạn!" });
            }

            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã hủy lời mời kết bạn!",
                relationshipStatus = "none"
            });
        }
        public async Task<IActionResult> LoadConversationsPanel(int? activeConversationId = null)
        {
            var conversations = await GetUserConversationsAsync(CurrentUserId);
            ViewBag.CurrentUserId = CurrentUserId;
            ViewBag.ActiveConversationId = activeConversationId ?? 0;

            // ✅ Trả về TOÀN BỘ _ConversationsPanel (bao gồm header, search, tabs, list)
            return PartialView("_ConversationsPanel", conversations);
        }

        // GET: Chat/GetMessageReactions - Lấy reactions của 1 tin nhắn
        public async Task<IActionResult> GetMessageReactions(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return Json(new { success = false, message = "Tin nhắn không tồn tại!" });
            }

            // Kiểm tra quyền truy cập conversation
            if (!await IsConversationMemberAsync(message.ConversationId, CurrentUserId))
            {
                return Json(new { success = false, message = "Không có quyền truy cập!" });
            }

            // Lấy reactions đã được group
            var reactions = await _context.MessageReactions
                .Where(r => r.MessageId == messageId)
                .GroupBy(r => r.ReactionType)
                .Select(g => new
                {
                    reactionType = g.Key,
                    count = g.Count(),
                    userIds = g.Select(r => r.UserId).ToList()
                })
                .ToListAsync();

            return Json(new { success = true, reactions = reactions });
        }
        // GET: Chat/SearchAll - Tìm kiếm cả users và conversations
        public async Task<IActionResult> SearchAll(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Vui lòng nhập từ khóa!" });
            }

            query = query.Trim().ToLower();

            // ✅ 1. Tìm Users (để kết bạn)
            var users = await _context.Users
                .Where(u => u.UserId != CurrentUserId
                        && (u.Email.ToLower().Contains(query)
                            || u.Username.ToLower().Contains(query)
                            || u.FullName.ToLower().Contains(query)
                            || (u.PhoneNumber != null && u.PhoneNumber.Contains(query))))
                .Select(u => new
                {
                    userId = u.UserId,
                    username = u.Username,
                    email = u.Email,
                    fullName = u.FullName,
                    avatar = u.Avatar ?? "/images/default-avatar.png",
                    isOnline = u.IsOnline ?? false
                })
                .Take(10)
                .ToListAsync();

            // Kiểm tra relationship với từng user
            var userIds = users.Select(u => u.userId).ToList();
            var friendIds = await _context.Friends
                .Where(f => f.UserId == CurrentUserId && userIds.Contains(f.FriendId))
                .Select(f => f.FriendId)
                .ToListAsync();

            var sentRequestIds = await _context.FriendRequests
                .Where(fr => fr.SenderId == CurrentUserId
                          && userIds.Contains(fr.ReceiverId)
                          && fr.Status == "pending")
                .Select(fr => fr.ReceiverId)
                .ToListAsync();

            var receivedRequestIds = await _context.FriendRequests
                .Where(fr => fr.ReceiverId == CurrentUserId
                          && userIds.Contains(fr.SenderId)
                          && fr.Status == "pending")
                .Select(fr => fr.SenderId)
                .ToListAsync();

            var usersResult = users.Select(u => new
            {
                type = "user",
                userId = u.userId,
                username = u.username,
                fullName = u.fullName,
                avatar = u.avatar,
                isOnline = u.isOnline,
                relationshipStatus = GetRelationshipStatus(u.userId, friendIds, sentRequestIds, receivedRequestIds)
            }).ToList();

            // ✅ 2. Tìm Conversations/Groups hiện có
            var myConversationIds = await _context.ConversationMembers
                .Where(cm => cm.UserId == CurrentUserId && cm.DeletedAt == null)
                .Select(cm => cm.ConversationId)
                .ToListAsync();

            var conversations = await _context.Conversations
                .Where(c => myConversationIds.Contains(c.ConversationId)
                        && (c.ConversationName != null && c.ConversationName.ToLower().Contains(query)))
                .Select(c => new
                {
                    type = "conversation",
                    conversationId = c.ConversationId,
                    conversationName = c.ConversationName,
                    isGroup = c.IsGroup ?? false
                })
                .Take(10)
                .ToListAsync();

            // ✅ 3. Tìm theo tên thành viên trong conversations 1-1
            var oneOnOneConvs = await _context.ConversationMembers
                .Where(cm => cm.UserId == CurrentUserId
                          && cm.DeletedAt == null
                          && cm.Conversation.IsGroup == false)
                .Include(cm => cm.Conversation)
                    .ThenInclude(c => c.ConversationMembers)
                        .ThenInclude(m => m.User)
                .ToListAsync();

            var matchingOneOnOne = oneOnOneConvs
                .Where(cm =>
                {
                    var otherMember = cm.Conversation.ConversationMembers
                        .FirstOrDefault(m => m.UserId != CurrentUserId && m.DeletedAt == null);

                    if (otherMember?.User == null) return false;

                    var fullName = otherMember.User.FullName?.ToLower() ?? "";
                    var username = otherMember.User.Username?.ToLower() ?? "";

                    return fullName.Contains(query) || username.Contains(query);
                })
                .Select(cm => new
                {
                    type = "conversation",
                    conversationId = cm.ConversationId,
                    conversationName = cm.Conversation.ConversationMembers
                        .FirstOrDefault(m => m.UserId != CurrentUserId && m.DeletedAt == null)
                        ?.User?.FullName ?? "Người dùng",
                    isGroup = false
                })
                .ToList();

            // ✅ Gộp kết quả
            var allResults = new
            {
                users = usersResult,
                conversations = conversations.Concat(matchingOneOnOne).DistinctBy(c => c.conversationId).ToList()
            };

            return Json(new { success = true, data = allResults });
        }
        // GET: Chat/GetConversationWithUser - Lấy conversationId với 1 user cụ thể
        public async Task<IActionResult> GetConversationWithUser(int userId)
        {
            // Tìm conversation 1-1 giữa current user và userId
            var conversation = await _context.ConversationMembers
                .Where(cm => cm.UserId == CurrentUserId && cm.DeletedAt == null)
                .Select(cm => cm.Conversation)
                .Where(c => c.IsGroup == false)
                .Where(c => c.ConversationMembers.Any(m => m.UserId == userId && m.DeletedAt == null))
                .FirstOrDefaultAsync();

            if (conversation == null)
            {
                return Json(new { success = false, message = "Không tìm thấy cuộc hội thoại!" });
            }

            return Json(new { success = true, conversationId = conversation.ConversationId });
        }
        // POST: Chat/AcceptFriendRequest - Chấp nhận lời mời kết bạn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriendRequest(int senderId)
        {
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(fr => fr.SenderId == senderId
                                        && fr.ReceiverId == CurrentUserId
                                        && fr.Status == "pending");

            if (request == null)
            {
                return Json(new { success = false, message = "Không tìm thấy lời mời kết bạn!" });
            }

            // Cập nhật status
            request.Status = "accepted";
            request.RespondedAt = DateTime.Now;

            // Tạo friendship 2 chiều
            var friendship1 = new Friend
            {
                UserId = CurrentUserId,
                FriendId = senderId,
                CreatedAt = DateTime.Now
            };

            var friendship2 = new Friend
            {
                UserId = senderId,
                FriendId = CurrentUserId,
                CreatedAt = DateTime.Now
            };

            _context.Friends.AddRange(friendship1, friendship2);

            // Tạo conversation 1-1
            var conversation = new Conversation
            {
                IsGroup = false,
                CreatedBy = CurrentUserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            var member1 = new ConversationMember
            {
                ConversationId = conversation.ConversationId,
                UserId = CurrentUserId,
                JoinedAt = DateTime.Now
            };

            var member2 = new ConversationMember
            {
                ConversationId = conversation.ConversationId,
                UserId = senderId,
                JoinedAt = DateTime.Now
            };

            _context.ConversationMembers.AddRange(member1, member2);

            // Tạo notification cho sender
            var notification = new Notification
            {
                UserId = senderId,
                NotificationType = "friend_accepted",
                Content = $"{CurrentUserFullName} đã chấp nhận lời mời kết bạn của bạn",
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã chấp nhận lời mời kết bạn!",
                relationshipStatus = "friend"
            });
        }

        // GET: Chat/LoadConversationsList - Load danh sách conversations cho sidebar
        //public async Task<IActionResult> LoadConversationsList()
        //{
        //    var conversations = await GetUserConversationsAsync(CurrentUserId);
        //    ViewBag.CurrentUserId = CurrentUserId;
        //    return PartialView("_ConversationsList", conversations);
        //}

        // GET: Chat/GetConversationItem - Lấy 1 conversation item (để update real-time)
        public async Task<IActionResult> GetConversationItem(int conversationId)
        {
            var member = await _context.ConversationMembers
                .AsNoTracking()
                .Where(cm => cm.ConversationId == conversationId && cm.UserId == CurrentUserId && cm.DeletedAt == null)
                .Include(cm => cm.Conversation)
                .FirstOrDefaultAsync();

            if (member == null)
            {
                return Json(new { success = false });
            }

            var conversation = member.Conversation;

            // Get members
            var members = await _context.ConversationMembers
                .Where(m => m.ConversationId == conversationId)
                .Select(m => new
                {
                    UserId = m.UserId,
                    Username = m.User.Username ?? "Người dùng đã xóa",
                    FullName = m.User.FullName ?? "Người dùng đã xóa",
                    Avatar = m.User.Avatar != null && m.User.Avatar.Trim() != ""
                        ? m.User.Avatar
                        : "/images/default-avatar.png",
                    IsOnline = m.User.IsOnline ?? false,
                    IsDeleted = m.DeletedAt != null
                })
                .ToListAsync();

            // Get last message
            var lastMessage = await _context.Messages
                .Where(msg => msg.ConversationId == conversationId
                           && msg.IsDeleted == false
                           && (member.DeletedAt == null || msg.SentAt > member.DeletedAt))
                .OrderByDescending(msg => msg.SentAt)
                .Select(msg => new
                {
                    msg.MessageText,
                    msg.SentAt,
                    msg.SenderId
                })
                .FirstOrDefaultAsync();

            // Get unread count
            var unreadCount = await _context.Messages
                .CountAsync(msg => msg.ConversationId == conversationId
                                && msg.SentAt > (member.LastReadAt ?? DateTime.MinValue)
                                && (member.DeletedAt == null || msg.SentAt > member.DeletedAt)
                                && msg.SenderId != CurrentUserId
                                && msg.IsDeleted == false);

            return Json(new
            {
                success = true,
                conversation = new
                {
                    conversationId = conversation.ConversationId,
                    conversationName = conversation.ConversationName,
                    isGroup = conversation.IsGroup,
                    isPinned = member.IsPinned ?? false,
                    isArchived = member.IsArchived ?? false,
                    lastReadAt = member.LastReadAt,
                    updatedAt = conversation.UpdatedAt,
                    members = members,
                    lastMessage = lastMessage,
                    unreadCount = unreadCount
                }
            });
        }
    }
}
