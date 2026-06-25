using Microsoft.AspNetCore.SignalR;

namespace MyEccomerce.Hubs
{
    public class StatusHub : Hub
    {

        public async Task JoinUpdateGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Updates");
        }


        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
    }

}