using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data; // I-check kung sakto imong DbContext namespace

namespace MyEccomerce.Hubs
{
    public class TrackingHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public TrackingHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, orderId);
        }

        public async Task UpdateRiderLocation(string orderIdGenerated, double lat, double lng)
        {
            // 1. Panggitaon ang Order sa DB
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderIdGenerated == orderIdGenerated);

            if (order != null)
            {
                // Assign sa bag-ong coordinates
                order.Latitude = lat;
                order.Longitude = lng;

                // Pugson ang EF Core nga i-mark as modified
                _context.Entry(order).State = EntityState.Modified;

                // I-save sa Database
                await _context.SaveChangesAsync();
            }

            // 2. Broadcast gihapon sa Customer real-time
            await Clients.Group(orderIdGenerated).SendAsync("ReceiveLocation", lat, lng);
        }
    }
}