using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Hubs;
using MyEccomerce.Models;

namespace MyEccomerce.Controllers.Api
{

    namespace MyEccomerce.Controllers.Api
    {
        [ApiController]
        // TANGTANGA ang [Route("api/[controller]")] diri
        public class AdminOrdersController : ControllerBase
        {
            private readonly ApplicationDbContext _context;
            private readonly IHubContext<StatusHub> _hubContext;
            public AdminOrdersController (ApplicationDbContext context, IHubContext<StatusHub> hubContext)
            {
                _context = context;
                _hubContext = _hubContext;
            }

            [HttpGet]
            [Route("api/admin/orders")]
            public IActionResult GetOrdersApi()
            {
                try
                {
                    var orders = _context.Orders
                        .Include(o => o.User)
                        .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                        .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant)
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => new
                        {
                            id = o.OrderId,
                            orderDate = o.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                            customerName = o.User != null ? (o.User.FirstName + " " + o.User.LastName) : "Guest",
                            customerEmail = o.User != null ? o.User.Email : "No Email",
                            totalAmount = o.TotalAmount,
                            status = o.Status ?? "Pending",
                            orderItems = o.OrderItems.Select(oi => new
                            {
                                productName = oi.Product != null ? oi.Product.Name : "Unknown Product",
                                variantName = oi.Variant != null ? oi.Variant.VariationName : "Standard",

                                // ======================================================================
                                // KINI ANG PINAKABAG-ONG FIX: FALLBACK LOGIC PARA SA STANDARD PRODUCTS
                                // ======================================================================
                                variantImage = oi.Variant != null && !string.IsNullOrEmpty(oi.Variant.ImageUrl)
                                    ? oi.Variant.ImageUrl // Plan A: Image sa Variant (Kopiko Pouch/Twin)
                                    : (oi.Product != null && !string.IsNullOrEmpty(oi.Product.ImageUrl)
                                        ? oi.Product.ImageUrl // Plan B: Image sa Main Product (Standard Items sama sa Pancit Canton)
                                        : "/images/no-image.png"), // Plan C: Default placeholder kon blangko gyud silang duha
                                                                   // ======================================================================

                                quantity = oi.Quantity,
                                price = oi.Price,
                                subTotal = oi.Quantity * oi.Price
                            }).ToList()
                        })
                        .ToList();

                    return Ok(orders);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Error loading orders", details = ex.Message });
                }
            }

            [HttpPost("api/orders/{orderId}/status")]
            public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] string status)
            {
                // 1. Siguroha nga ang status dili null o blangko (Luwas gikan sa binding issues)
                if (string.IsNullOrWhiteSpace(status))
                {
                    return BadRequest(new { message = "Status cannot be empty. Susiha ang gipasa sa Android, boss." });
                }

                var order = await _context.Orders.FindAsync(orderId);

                // 2. Luwas nga susihon kung naa ba gyud ang order sa DB
                if (order == null)
                {
                    return NotFound(new { message = $"Order with ID {orderId} not found." });
                }

                // 3. Siguroha nga ang Order adunay kauban nga UserId aron malikayan ang NullReference sa SignalR
                if (order.UserId == null)
                {
                    return BadRequest(new { message = $"CRITICAL: Ang Order #{orderId} walay UserId nga nakasumpay sa database!" });
                }

                try
                {
                    order.Status = status;

                    // Pag-set sa Note depende sa Status
                    string statusNote = status switch
                    {
                        "Confirmed" => "Gi-check na sa seller ang imong order ug gi-andam na.",
                        "Out for Delivery" => "Ang imong order gi-turn over na sa courier ug padulong na kanimo.",
                        "Completed" => "Salamat! Nadawat na ang order. Hinaot nagustohan nimo!",
                        "Cancelled" => "Ang imong order na-cancel. Kontaka ang admin kung naay pangutana.",
                        "Pending" => "Nagpaabot pa sa confirmation gikan sa seller.",
                        _ => $"Ang status sa imong order na-update sa {status}."
                    };

                    // Create Notification Object
                    var newNotif = new Models.Notification
                    {
                        OrderId = orderId,
                        UserId = order.UserId.ToString(), // Luwas na kay gi-check na sa taas
                        Message = $"Ang imong Order #{orderId} kay {status} na!",
                        Status = status,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        UserProfilePicture = "https://ui-avatars.com/api/?name=Admin&background=0D6EFD&color=fff",
                        TargetUrl = $"/Orders/OrderDetails/{orderId}"
                    };

                    // Create Order Log (Timeline)
                    _context.OrderLogs.Add(new OrderLog
                    {
                        OrderId = orderId,
                        Status = status,
                        Note = statusNote,
                        LogDate = DateTime.UtcNow
                    });

                    _context.Notifications.Add(newNotif);

                    // I-save sa database
                    await _context.SaveChangesAsync();

                    // 4. Luwas nga pagkuha sa User ID para sa SignalR Group
                    string orderOwnerId = order.UserId.ToString();

                    // 5. Luwas nga pag-trigger sa SignalR Context (Susiha kung na-inject ba)
                    if (_hubContext != null && _hubContext.Clients != null)
                    {
                        await _hubContext.Clients.Group(orderOwnerId).SendAsync("NewUpdateAlert", new
                        {
                            orderId = orderId,
                            status = status,
                            note = statusNote,
                            message = newNotif.Message,
                            targetUrl = newNotif.TargetUrl
                        });
                    }
                    else
                    {
                        Console.WriteLine("WARNING: Ang _hubContext kay NULL! Wala ma-inject og tarong sa Constructor.");
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Order status updated successfully.",
                        currentStatus = status
                    });
                }
                catch (Exception ex)
                {
                    // Kung naay laing error (e.g., Database issue), makuha gihapon nato ang detalye sa ex.InnerException
                    var internalMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return StatusCode(500, new { success = false, message = internalMessage });
                }
            }
        }
    }
}
