using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using System.Security.Claims;

namespace MyEccomerce.Controllers
{
    

        public class RiderController : Controller
        {
            private readonly ApplicationDbContext _context;

            public RiderController(ApplicationDbContext context)
            {
                _context = context;
            }

            public async Task<IActionResult> Rider()
            {
            var currentRiderId = 14;//Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Kuhaon ang active order sa naka-login nga rider
                var activeOrder = await _context.Orders.FirstOrDefaultAsync(o => o.RiderId == currentRiderId && o.Status == "Out for Delivery");


            return View("~/Pages/Rider/RiderDashboard.cshtml", activeOrder);
            }


        }
    
}