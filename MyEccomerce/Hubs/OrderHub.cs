using Microsoft.AspNetCore.SignalR;

namespace MyEccomerce.Hubs
{
    public class OrderHub : Hub
    {
        // Pinalitan para tumanggap ng userId
        public async Task JoinAdminGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
        }

        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
    }
}