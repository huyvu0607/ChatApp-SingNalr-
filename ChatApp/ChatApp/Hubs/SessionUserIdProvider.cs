using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Hubs
{
    /// <summary>
    /// Custom UserIdProvider để SignalR lấy UserId từ Session
    /// </summary>
    public class SessionUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // ✅ Lấy UserId từ Session
            var httpContext = connection.GetHttpContext();
            if (httpContext != null)
            {
                var userId = httpContext.Session.GetInt32("UserId");
                if (userId.HasValue && userId.Value > 0)
                {
                    Console.WriteLine($"✅ [SessionUserIdProvider] Got UserId: {userId.Value}");
                    return userId.Value.ToString();
                }
                else
                {
                    Console.WriteLine(" ⚠️ [SessionUserIdProvider] UserId not found in Session");
                }
            }
            else
            {
                Console.WriteLine("❌ [SessionUserIdProvider] HttpContext is null!");
            }

            return null;
        }
    }
}