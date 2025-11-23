📩 ChatApp – Ứng dụng Chat Realtime bằng ASP.NET Core MVC + SignalR

ChatApp là một ứng dụng trò chuyện thời gian thực được xây dựng bằng ASP.NET Core MVC, Entity Framework Core, SignalR, và kiến trúc chuẩn MVC. Ứng dụng hỗ trợ chat cá nhân, chat nhóm, thông báo realtime, lưu tin nhắn, chỉnh sửa, reaction, và quản lý danh sách bạn bè.

🚀 Công nghệ sử dụng
Công nghệ	Mô tả
ASP.NET Core MVC	Xây dựng web app theo mô hình MVC
SignalR	Xử lý realtime: gửi/nhận tin nhắn ngay lập tức
EF Core	ORM quản lý database (SQL Server)
Bootstrap 5	UI responsive
jQuery / JavaScript	Xử lý giao diện & AJAX
Razor Views	Render giao diện linh hoạt
SQL Server	Lưu trữ dữ liệu chat và thông tin người dùng
📁 Cấu trúc thư mục
├── Controllers
│   ├── AuthController.cs
│   ├── BaseController.cs
│   ├── ChatController.cs
│   └── HomeController.cs
├── Extensions
│   └── ControllerExtensions.cs
├── Hubs
│   ├── ChatHub.cs
│   └── SessionUserIdProvider.cs
├── Models
│   ├── ChatAppContext.cs
│   ├── Conversation.cs
│   ├── ConversationMember.cs
│   ├── Message.cs
│   ├── MessageEditHistory.cs
│   ├── MessageReaction.cs
│   ├── SavedMessage.cs
│   ├── Friend.cs
│   ├── FriendRequest.cs
│   ├── Notification.cs
│   ├── User.cs
│   └── ErrorViewModel.cs
├── Views (UI)
│   ├── Auth (Login / Register)
│   ├── Chat (Giao diện chat realtime)
│   ├── Home (Trang chủ, Privacy)
│   └── Shared (Layout + Error)
├── wwwroot
│   ├── css
│   ├── js (chat.js, chat-search.js)
│   └── lib (Bootstrap, jQuery)
├── appsettings.json
├── Program.cs
└── ChatApp.csproj

🧩 Chức năng chính
🔐 1. Xác thực người dùng

Đăng nhập / đăng ký bằng username & password

Lưu session user

Tự động redirect nếu chưa đăng nhập

💬 2. Chat Realtime (SignalR)

Gửi tin nhắn realtime

Nhận tin ngay không cần reload

Hiển thị "đang nhập…"

Seen message

Chat nhóm & chat riêng

Tự cập nhật danh sách hội thoại

🧑‍🤝‍🧑 3. Quản lý bạn bè

Gửi yêu cầu kết bạn

Chấp nhận / từ chối

Xóa bạn

Danh sách Friends, FriendRequests

📨 4. Tin nhắn nâng cao

Chỉnh sửa tin nhắn

Xem lịch sử chỉnh sửa

React tin nhắn (icon cảm xúc)

Xóa / thu hồi tin nhắn

Lưu tin nhắn vào Saved Messages

🔔 5. Thông báo realtime

Thông báo: kết bạn, tin nhắn mới, thêm vào nhóm

Badge thông báo không đọc

Push realtime qua SignalR

👥 6. Nhóm chat

Tạo group

Thêm thành viên

Và rời / xoá nhóm

Hiển thị danh sách thành viên nhóm

⚙️ Cấu hình
1. Kết nối database

Trong appsettings.json:

"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=ChatAppDB;Trusted_Connection=True;"
}

2. Chạy migration
dotnet ef database update

▶️ Chạy dự án
Bằng CLI:
dotnet run

Hoặc trong Visual Studio:

F5 → chạy với IIS Express hoặc Kestrel

📡 SignalR Endpoint

Trong Program.cs:

app.MapHub<ChatHub>("/chatHub");


Frontend kết nối:

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

🧪 API & luồng hoạt động chính
🟦 Đăng nhập

POST /Auth/Login

🟦 Gửi tin nhắn

SignalR Method: SendMessage(conversationId, message)

🟦 Tạo group chat

POST /Chat/CreateGroup

🟦 Tải tin nhắn

GET /Chat/Conversation/{id}

📷 Giao diện (Views)

Chat/Index.cshtml: giao diện danh sách hội thoại

Chat/conversation.cshtml: view phòng chat

Chat/_CreateGroupModal.cshtml: modal tạo nhóm

Auth/Login.cshtml – Auth/Register.cshtml

🛠 Hướng dẫn phát triển mở rộng

Bạn có thể dễ dàng thêm:

Chat video (WebRTC)

Dark mode

Push notification mobile

Upload file, ảnh, video

Status online/offline

Chatbot AI

🧑‍💻 Tác giả

ChatApp – xây dựng bằng ASP.NET Core MVC + SignalR
Người phát triển: huyvu0607