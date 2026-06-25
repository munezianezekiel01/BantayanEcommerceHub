using Microsoft.AspNetCore.SignalR;

namespace MyEccomerce.Hubs
{
    public class TrackingHub : Hub
    {
        // 1. Join Group: Ang customer mo-join sa group base sa ilang OrderId
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, orderId);
        }

        // 2. Update Location: Ang Rider App/Page mo-tawag ani para i-broadcast ang coordinates
        public async Task UpdateRiderLocation(string orderId, double lat, double lng)
        {
            // I-send ra sa mga tawo nga naa sulod sa maong OrderId nga group
            await Clients.Group(orderId).SendAsync("ReceiveLocation", lat, lng);
        }
    
}
}
