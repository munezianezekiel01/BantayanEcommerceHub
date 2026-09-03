using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Hubs;
using System.Security.Claims;

namespace MyEccomerce.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RiderApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<StatusHub> _hubContext;

        public RiderApiController(ApplicationDbContext context, IHubContext<StatusHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/Rider/Dashboard
        [HttpGet("Dashboard")]
        public async Task<IActionResult> GetRiderDashboard()
        {
            //var riderIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            String riderIdClaim = "14";

            if (string.IsNullOrEmpty(riderIdClaim) || !int.TryParse(riderIdClaim, out int currentRiderId))
            {
                return Unauthorized(new { message = "Invalid o missing user credentials." });
            }

            // 1. Fetch orders allocated to the rider
            var orders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.RiderId == currentRiderId &&
                           (o.Status == "Assigned" || o.Status == "Out for Delivery"))
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderIdGenerated,
                    o.Status,
                    o.OrderDate,
                    o.TotalAmount,
                    CustomerName = o.User != null ? o.User.FirstName : "Customer",
                    CustomerPhone = o.User != null ? o.User.Phone : "",
                    ShippingAddress = o.DeliveryAddress
                })
                .ToListAsync();

            // 2. Identify Active Order for GPS tracking
            var activeOrder = orders.FirstOrDefault(o => o.Status == "Out for Delivery")
                           ?? orders.FirstOrDefault(o => o.Status == "Assigned");

            string activeOrderId = activeOrder?.OrderIdGenerated ?? activeOrder?.OrderId.ToString();

            // 3. Return structured JSON payload
            return Ok(new
            {
                success = true,
                activeOrderId = activeOrderId,
                totalAssignedOrders = orders.Count,
                orders = orders
            });
        }
    }
}
