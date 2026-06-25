using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using System.Security.Claims;

namespace MyEccomerce.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kani modagan sa matag tawag sa bisan unsang Action.
        /// Gigamit ni aron ang Badge Counts (Cart & Notifs) permi naay sulod sa tibuok website.
        /// </summary>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Kuhaa ang Unique ID sa user gikan sa Claims (Google ID o Identity ID)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userIdStr))
            {
                try
                {
                    // 2. Pangitaa ang user sa Database gamit ang GoogleId/IdentityId
                    var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == userIdStr);

                    if (dbUser != null)
                    {
                        ViewBag.CurrentUser = dbUser;

                        // 3. I-load ang Cart Count (Gamit ang Integer UserId gikan sa DB)
                        ViewBag.CartCount = await _context.Carts
                            .CountAsync(c => c.UserId == dbUser.UserId);

                        // 4. I-load ang Unread Notification Count
                        // Dinhi, i-check kon ang imong Notif table naggamit ba og string userId o int
                        // Gi-assume nako nga string ang UserId sa Notifications table base sa imong karaan nga code
                        ViewBag.UnreadNotifCount = await _context.Notifications
                            .CountAsync(n => n.UserId == userIdStr && !n.IsRead);
                    }
                    else
                    {
                        SetDefaultViewBag();
                    }
                }
                catch (Exception ex)
                {
                    // Kon naay error sa DB, i-set lang sa 0 para dili mag-crash ang page
                    SetDefaultViewBag();
                    Console.WriteLine("BaseController Error: " + ex.Message);
                }
            }
            else
            {
                SetDefaultViewBag();
            }

            // Padayon sa pag-execute sa actual Action (e.g. Home, Cart, etc.)
            await next();
        }

        private void SetDefaultViewBag()
        {
            ViewBag.CartCount = 0;
            ViewBag.UnreadNotifCount = 0;
            ViewBag.CurrentUser = null;
        }
    }
}