using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ChatApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Controllers
{
    /// <summary>
    /// Base Controller cho tất cả controllers cần authentication
    /// Tự động check session và cung cấp thông tin user hiện tại
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly ChatAppContext _context;

        // Properties để truy cập thông tin user hiện tại dễ dàng
        protected int CurrentUserId
        {
            get
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                return userId ?? 0;
            }
        }

        protected string CurrentUsername
        {
            get
            {
                return HttpContext.Session.GetString("Username") ?? string.Empty;
            }
        }

        protected string CurrentUserFullName
        {
            get
            {
                return HttpContext.Session.GetString("FullName") ?? string.Empty;
            }
        }

        protected string CurrentUserAvatar
        {
            get
            {
                return HttpContext.Session.GetString("Avatar") ?? "/images/default-avatar.png";
            }
        }

        protected bool IsAuthenticated
        {
            get
            {
                return HttpContext.Session.GetInt32("UserId") != null;
            }
        }

        // Constructor
        public BaseController(ChatAppContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tự động kiểm tra authentication trước mỗi action
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Kiểm tra xem user đã đăng nhập chưa
            if (!IsAuthenticated)
            {
                // Chưa đăng nhập -> redirect về trang login
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Lưu thông tin user vào ViewBag để dùng trong View
            ViewBag.CurrentUserId = CurrentUserId;
            ViewBag.CurrentUsername = CurrentUsername;
            ViewBag.CurrentUserFullName = CurrentUserFullName;
            ViewBag.CurrentUserAvatar = CurrentUserAvatar;

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Helper method: Lấy thông tin user hiện tại từ database
        /// </summary>
        protected async Task<User?> GetCurrentUserAsync()
        {
            return await _context.Users.FindAsync(CurrentUserId);
        }

        /// <summary>
        /// Helper method: Hiển thị thông báo success
        /// </summary>
        protected void ShowSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        /// <summary>
        /// Helper method: Hiển thị thông báo error
        /// </summary>
        protected void ShowErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        /// <summary>
        /// Helper method: Hiển thị thông báo info
        /// </summary>
        protected void ShowInfoMessage(string message)
        {
            TempData["InfoMessage"] = message;
        }

        /// <summary>
        /// Helper method: Kiểm tra xem 2 user có phải bạn bè không
        /// </summary>
        protected async Task<bool> IsFriendAsync(int userId1, int userId2)
        {
            return await _context.Friends
                .AnyAsync(f => f.UserId == userId1 && f.FriendId == userId2);
        }

        /// <summary>
        /// Helper method: Kiểm tra user có phải admin của conversation không
        /// </summary>
        protected async Task<bool> IsConversationAdminAsync(int conversationId, int userId)
        {
            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

            return member?.IsAdmin ?? false;
        }

        /// <summary>
        /// Helper method: Kiểm tra user có phải thành viên của conversation không
        /// </summary>
        protected async Task<bool> IsConversationMemberAsync(int conversationId, int userId)
        {
            return await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
        }
    }
}