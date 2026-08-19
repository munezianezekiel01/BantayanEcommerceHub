using Microsoft.AspNetCore.SignalR;


namespace MyEccomerce.Hubs
{
    public class OrderHub :Hub
    {
         
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }


        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

           
        }

        
    }
}
