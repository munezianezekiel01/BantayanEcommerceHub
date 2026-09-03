using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Hubs;
using MyEccomerce.Models;
using System.Net.NetworkInformation;
using System.Security.Claims;

namespace MyEccomerce.Controllers
{
    

        public class RiderController : Controller
        {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<StatusHub> _hubContext;
        public RiderController(ApplicationDbContext context, IHubContext<StatusHub> hubContext)
            {
                _context = context;
                _hubContext = hubContext;
            }

        public async Task<IActionResult> Rider()
        {
            var riderIdClaim =  User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(riderIdClaim)) return RedirectToAction("Login", "Account");

            int currentRiderId = int.Parse(riderIdClaim); //14


            
            var orders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.RiderId == currentRiderId &&
                           (o.Status == "Assigned" || o.Status == "Out for Delivery"))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();



            

            // Kuhaa ang Active Order ID para sa GPS Tracking sa Layout
            var activeOrder = orders.FirstOrDefault(o => o.Status == "Out for Delivery")
                           ?? orders.FirstOrDefault(o => o.Status == "Assigned");




          
            // 1. Pag-set sa Note depende sa Status



            ViewBag.ActiveOrderId = activeOrder?.OrderIdGenerated ?? activeOrder?.OrderId.ToString();

            return View("~/Pages/Rider/RiderDashboard.cshtml", orders);
        }


        

       
    }
    
}