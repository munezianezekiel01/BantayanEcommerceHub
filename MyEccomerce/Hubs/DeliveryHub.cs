using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Models;

namespace MyEccomerce.Hubs
{
    
    public class DeliveryHub :Hub
    {
        private readonly ApplicationDbContext _context;

        public DeliveryHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinOrderGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Updates");
        }

        public async Task OrderStatus(string userId)
        {
            // 1. Panggitaon ang Order sa DB
           

            await Groups.AddToGroupAsync(Context.ConnectionId, userId);


           
        }

    }
}
