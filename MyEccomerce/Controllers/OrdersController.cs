using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Hubs;
using MyEccomerce.Models; // Siguroha nga husto ang imong namespace
using Rotativa.AspNetCore;
using System.Security.Claims;

public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly   IHubContext<OrderHub> _hubContext;

    public OrdersController(ApplicationDbContext context, IHubContext<OrderHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // GET: Orders/MyOrders
    public async Task<IActionResult> MyOrders()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
        int userId = int.Parse(userIdString);
        // o unsaon nimo pagkuha ang ID

        // Kuhaon ang pinakabag-o nga order sa user
        var latestOrder = _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefault();

        // Kung naay order, kuhaon iyang status (e.g. "Confirmed"), kung wala, "Pending" ang default
        ViewBag.CurrentOrderStatus = latestOrder != null ? latestOrder.Status : "Pending";

        

        // 1. OPTIMIZATION: .AsNoTracking() ug Gi-usa ra ang query lakip ang OrderLogs!
        var myOrders = await _context.Orders
            .AsNoTracking() // Makapagaan kaayo sa memory cache
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant)
            .Include(o => o.User)
            .Include(o => o.OrderLogs) // <--- KINI ANG BAG-O: Gi-load na diritso diri kausa ra!
            .Where(o => o.UserId == userId && !o.IsDeleted)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        // 2. Kuhaon ang TargetStatus gikan sa ArchiveOrder action (via TempData)
        ViewBag.TargetStatus = TempData["TargetStatus"] ?? "Pending";

        // 3. LOG LOGIC FIX:
        // Gi-sort na lang nato ang logs sa memorya (LINQ to Objects) imbes nga mag-query pa sa database!
        foreach (var order in myOrders)
        {
            if (order.OrderLogs != null)
            {
                order.OrderLogs = order.OrderLogs
                    .OrderByDescending(l => l.LogDate)
                    .ToList();
            }
        }

        return View("~/Pages/Public/MyOrders.cshtml", myOrders);
    }



    [HttpGet]
    [Route("Orders/GetOrderId/{OrderId}")]
    public async Task<IActionResult> GetOrderId(int OrderId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

        int userId = int.Parse(userIdString);

        // 1. FIX & OPTIMIZATION: Gigamitan og FirstOrDefaultAsync imbes nga ToListAsync
        // para usa ra ka piho nga Order object ang kuhaon sa memory, dili List.
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant)
            .Include(o => o.OrderLogs)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.OrderId == OrderId);

        if (order == null) return NotFound();

        // Kuhaon ang status para sa JS filter
        ViewBag.TargetStatus = order.Status;

        // 2. OPTIMIZATION: Limpyo ug paspas nga pag-update sa notifications
        var unreadNotifs = await _context.Notifications
            .Where(n => n.OrderId == OrderId && !n.IsRead)
            .ToListAsync(); // Ayaw butangi og AsNoTracking diri kay ato man i-update

        if (unreadNotifs.Any())
        {
            foreach (var notif in unreadNotifs)
            {
                notif.IsRead = true;
            }

            // TANGTANG ANG UpdateRange(): Dili na kinahanglan kay tracked na kini sa EF Core.
            await _context.SaveChangesAsync();
        }

        // 3. UI FIX FOR VIEW:
        // Tungod kay ang "MyOrders.cshtml" nag-abot og Listahan (IEnumerable<Order>),
        // ato kining isulod og bag-ong List para dili maguba ang `@foreach` loop sa imong View!
        var modelList = new List<Order> { order };

        return View("~/Pages/Public/MyOrders.cshtml", modelList);
    }



    //OPTIMIZED QUERY
    
    [HttpPost]
    [ValidateAntiForgeryToken] //
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            return Json(new { success = false, message = "Palihog login una, boss." });

        int userId = int.Parse(userIdString);

        // 1. SECURITY & DATA VALIDATION
        var order = await _context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        if (order == null)
            return Json(new { success = false, message = "Wala makita ang order, boss." });

        if (order.Status != "Pending")
        {
            return Json(new { success = false, message = "Dili na pwede ma-cancel kini nga order, Boss." });
        }

        // 2. MAG-SUGOD OG TRANSACTION
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 3. UPDATE STATUS SA ORDER TABLE
            order.Status = "Cancelled";

            // 4. LOG THE EVENT (Gi-wasto sumala sa imong tinuod nga OrderLog properties)
            var log = new OrderLog
            {
                OrderId = orderId,
                Status = "Cancelled", // Gi-apil nato ang Status column sa log
                Note = "Gi-cancel sa kustomer ang order.", // Sakto na kini, 'Note' gyud!
                LogDate = DateTime.Now
            };
            _context.OrderLogs.Add(log);

            // Save Entity Framework Tracking changes (Order & OrderLog)
            await _context.SaveChangesAsync();

            // 5. INSERT NOTIFICATION TO DATABASE (Direct SQL)
            string sql = @"INSERT INTO Notifications (UserId, Message, TargetUrl, UserProfilePicture, CreatedAt, IsRead) 
                       VALUES ({0}, {1}, {2}, {3}, {4}, {5})";

            string profilePic = !string.IsNullOrEmpty(order.User?.ImageUrl)
                                ? order.User.ImageUrl
                                : $"https://ui-avatars.com/api/?name={order.User?.FirstName}&background=random";

            await _context.Database.ExecuteSqlRawAsync(sql,
                "Admin",
                $"Gi-cancel ni {order.User?.FirstName ?? "usa ka Customer"} ang Order #{orderId}!",
                $"/Admin/Orders/Details/{orderId}",
                profilePic,
                DateTime.Now,
                false);

            // 6. COMMIT TRANSACTION: Iselyo na sa DB silang duha
            await transaction.CommitAsync();

            // 7. SIGNALR REAL-TIME PUSH
            await _hubContext.Clients.Group("Admins").SendAsync("OrderCancelledAlert", new
            {
                orderId = order.OrderId,
                customerName = order.User?.FirstName ?? "A Customer",
                status = "Cancelled"
            });

            return Json(new { success = true, message = "Order cancelled successfully!" });
        }
        catch (Exception ex)
        {
            // Rollback diritso kung naay mapakyas aron limpyo ang DB
            await transaction.RollbackAsync();

            // I-return ang ex.Message ug ex.InnerException para kung naay sayop sa database tables,
            // makita nimo diritso ang tinuod nga rason sa imong front-end JSON response.
            var innerError = ex.InnerException != null ? ex.InnerException.Message : "";
            return Json(new { success = false, message = $"Error: {ex.Message}. Inner: {innerError}" });
        }
    }

    [HttpGet]

    public async Task<IActionResult> GetStatusUpdates()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Json(new { unreadCount = 0, updates = new List<object>() });

        // 1. OPTIMIZATION: Gigamitan og .AsNoTracking() para sa paspas nga pag-ihap sa unread notifications
        var unreadCount = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        // 2. OPTIMIZATION: .AsNoTracking() ug Gi-optimize ang Join gamit ang Navigation Properties
        // Imbes nga mag-manual query ka sa `_context.OrderItems` sa sulod, mas maayo nga gamiton ang 
        // Navigation Property kung naa (pananglitan `n.Order.OrderItems`). Pero kung walay direct relationship, 
        // atong pabilinan ang gipagaan nga query sa ubos nga naay AsNoTracking:
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new {
                orderId = n.OrderId,
                message = n.Message,
                status = n.Status,
                isRead = n.IsRead,
                createdAt = n.CreatedAt,
                // Gipagaan nga sub-query gamit ang AsNoTracking
                orderItems = _context.OrderItems
                    .AsNoTracking()
                    .Where(oi => oi.OrderId == n.OrderId)
                    .Select(oi => new {
                        itemName = oi.VariantId != null
                            ? oi.Product.Name + " (" + oi.Variant.VariationName + ")"
                            : oi.Product.Name,
                        itemImage = (oi.VariantId != null && oi.Variant.ImageUrl != null)
                            ? oi.Variant.ImageUrl
                            : oi.Product.ImageUrl
                    })
                    .ToList()
            })
            .ToListAsync();

        return Json(new
        {
            currentUserId = userId,
            unreadCount,
            updates = notifications
        });
    }

    [HttpGet]
    [IgnoreAntiforgeryToken] // <--- I-DUGANG KINI para dili ka i-block sa Razor Pages security!
    [Route("Orders/MarkAsRead/{id:int}")] // <--- I-siguro nga int kini nga parameter
    public async Task<IActionResult> MarkAsRead([FromRoute] int id) // <--- I-EXPLICIT ANG [FromRoute]
    {
        try
        {
            // 1. Pangitaon ang specific notification gamit ang Notification ID
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                return Json(new { success = false, message = "Wala nakita ang notification." });
            }

            // 2. I-update ngadto sa Read
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync(); // Luwas na sa DB!
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: Orders/TrackOrder/5
    public async Task<IActionResult> TrackOrder(int id)
    {
        // Kuhaon ang order data base sa ID
        var order = await _context.Orders
            .FirstOrDefaultAsync(m => m.OrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        // Siguraduhon nato nga "Out for Delivery" ang status 
        // o depende sa imong logic kung gusto nimo ma-view gihapon bisan pending
        return View("~/Pages/Rider/TrackOrder.cshtml", order);
    }




    ////////////////////
    ///

    // GET: Orders/AssignRider/5
    // GET: Orders/AssignRider/5
    public async Task<IActionResult> AssignRider(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        // Kuhaon nato ang tanang users nga ang UserType kay "Rider"
        // Gidugangan nako og 'AsEnumerable' para sa SelectList compatibility
        var riders = await _context.Users
            .Where(u => u.UserType == "Rider")
            .Select(u => new {
                u.UserId,
                FullName = u.FirstName + " " + u.LastName
            }).ToListAsync();

        ViewBag.RiderList = new SelectList(riders, "UserId", "FullName");

        // Siguroha nga husto ang path sa imong CSHTML file
        return View("~/Views/Admin/AssignRider.cshtml", order);
    }

    // POST: Orders/AssignRider
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRider(int OrderId, int RiderId)
    {
        var order = await _context.Orders.FindAsync(OrderId);

        if (order == null) return NotFound();

        // I-update ang fields
        order.RiderId = RiderId;
        order.Status = "Out for Delivery";

        try
        {
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Usba ni kung unsa ang name sa imong main list view
            return RedirectToAction("Admin", "Orders");
        }
        catch (Exception ex)
        {
            // Kung naay error, i-log nato (optional)
            ModelState.AddModelError("", "Dili ma-save ang assignment: " + ex.Message);

            // Kinahanglan i-repopulate ang RiderList kung mo-balik sa View
            var riders = await _context.Users.Where(u => u.UserType == "Rider").ToListAsync();
            ViewBag.RiderList = new SelectList(riders, "UserId", "FirstName");
            return View(order);
        }
    }

    [HttpPost]
    
    public async Task<IActionResult> ArchiveOrder(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Gamit og TempData para mapasa ang "TargetStatus" sa sunod nga request
        TempData["TargetStatus"] = "Completed";

        // I-redirect balik sa Index action diin gina-list ang tanang orders
        return RedirectToAction(nameof(MyOrders));
    }

    

    public async Task<IActionResult> DownloadInvoice(int id)
{
    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
    int userId = int.Parse(userIdString);

    var order = await _context.Orders
        .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
        .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant)
        .Include(o => o.User)
        .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

    if (order == null) return NotFound();

    // Mao ni ang magic line:
    return new ViewAsPdf("Invoice", order)
    {
        FileName = $"Invoice_{order.OrderId}.pdf",
        PageSize = Rotativa.AspNetCore.Options.Size.A4,
        PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
        CustomSwitches = "--footer-center \"Pagpasalamat sa pagpalit sa BantayanHub!\" --footer-line --footer-font-size \"10\""
    };



}


    // GET: /Orders/OrderDetails/1240
    public async Task<IActionResult> OrderDetails(int id)
    {
        // 1. Kuhaa ang order lakip ang iyang mga items, variants, products, ug timeline logs
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
            .Include(o => o.OrderLogs)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        // 2. Kuhaa ang unread notifications para ani nga order (Tracked by EF Core)
        var unreadNotifs = await _context.Notifications
            .Where(n => n.OrderId == id && !n.IsRead)
            .ToListAsync();

        // 3. I-update diritso sa foreach (kon walay unread, mo-skip ra ni automatic)
        foreach (var notif in unreadNotifs)
        {
            notif.IsRead = true;
        }

        // 4. KINI ANG SAKTO: _context.SaveChangesAsync() ang tawgon, dili sa DbSet!
        if (unreadNotifs.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        // Pwede sab nimo i-secure diri nga ang tag-iya ra sa order ang maka-tan-aw
        // string currentUserId = _userManager.GetUserId(User);
        // if (order.UserId != currentUserId) return Unauthorized();

        return View("~/Pages/Public/OrderDetails.cshtml", order);
    }

}


