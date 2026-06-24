using Microsoft.AspNetCore.Mvc;
using MyEccomerce.Data;
using System.Linq; // Importante para sa ToList()

namespace MyEccomerce.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Kinahanglan ang Constructor para ma-access ang Database
        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            // 1. Kuhaon ang data gikan sa Database table nga 'Notifications'
            // I-order nato para ang pinakabag-o maoy naa sa taas
            var notifications = _context.Notifications
                                        .OrderByDescending(n => n.CreatedAt)
                                        .ToList();

            // 2. I-pass ang 'notifications' variable sa View
            return View( "~/Pages/Admin/Dashboard.cshtml", notifications);
        }


        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _context.Update(notification);
                await _context.SaveChangesAsync();
            }

            // Kon gusto nimo i-redirect ang user sa link sa notif (pananglitan: Order Details)
            if (!string.IsNullOrEmpty(notification.TargetUrl))
            {
                return Redirect(notification.TargetUrl);
            }

            return RedirectToAction("Index"); // Balik sa listahan kon walay URL
        }



        public IActionResult Notification()
        {
            return View("~/Pages/Public/Notifications.cshtml");
        }
    }
}