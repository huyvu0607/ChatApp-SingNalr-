using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ChatAppContext _context;

        public AuthController(ChatAppContext context)
        {
            _context = context;
        }

        // GET: Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập, chuyển về trang chủ
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Chat");
            }
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            // Hash password
            string hashedPassword = HashPassword(password);

            // Tìm user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == hashedPassword);

            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }

            // Lưu session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
            HttpContext.Session.SetString("Avatar", user.Avatar ?? "/images/default-avatar.png");

            // Cập nhật trạng thái online
            user.IsOnline = true;
            user.LastSeen = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Chat");
        }

        // GET: Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            // Nếu đã đăng nhập, chuyển về trang chủ
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Chat");
            }
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword, string fullName)
        {
            // Validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fullName))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự!";
                return View();
            }

            // Kiểm tra username đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                return View();
            }

            // Kiểm tra email đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Email đã được sử dụng!";
                return View();
            }

            // Tạo user mới
            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                FullName = fullName,
                Avatar = "/images/default-avatar.png",
                IsOnline = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Đăng ký thành công! Vui lòng đăng nhập.";
            return View("Login");
        }

        // GET: Auth/Logout
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                // Cập nhật trạng thái offline
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            // Xóa session
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        // Helper: Hash password bằng SHA256
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}