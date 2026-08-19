using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Models;
using System.Security.Claims;

namespace MyEccomerce.Controllers
{
   
    public class PurchaseHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PurchaseHistoryController(ApplicationDbContext context)
        {
            _context = context;

        }
        


        [HttpGet]
        [Route("PurchaseHistory/PurchaseSummary/{id}")]
        public async Task<IActionResult> PurchaseSummary(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdString);

            // I-fetch ang Order uban ang OrderItems ug Product details
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Wala makita ang summary sa order, boss.";
                //return RedirectToAction("Index", "Home");
            }

            // I-map ang order data ngadto sa PurchaseSummaryViewModel
            var viewModel = new PurchaseSummaryViewModel
            {
                OrderId = order.OrderId,
                CustomerName = $"{order.User?.FirstName} {order.User?.LastName}",
                PhoneNumber = order.User?.Phone ?? "N/A",
                ShippingAddress = order.DeliveryAddress ?? "Standard Delivery Address",
                // PaymentMethod = order.PaymentMethod ?? "Cash on Delivery",
                OrderStatus = order.Status,
                CreatedAt = order.OrderDate,
                // ShippingFee = order.,
                Items = order.OrderItems.Select(item => new PurchaseSummaryItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? "Product",
                    ImageUrl = item.Product?.ImageUrl ?? "/images/default-product.png",
                    UnitPrice = item.Price,
                    Quantity = item.Quantity
                }).ToList()
            };

            return View("~/Pages/Public/PurchaseHistory.cshtml", viewModel);
        }
    }
}
