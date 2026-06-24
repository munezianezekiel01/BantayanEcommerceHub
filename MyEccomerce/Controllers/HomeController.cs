using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Models;
using Org.BouncyCastle.Tls;
using System.Security.Claims;

namespace MyEccomerce.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }



        // Gidugangan nato og parameter para sa page (Default kay Page 1)
        public async Task<IActionResult> Home(int? id, string q, int page = 1)
        {
            ViewBag.SearchTerm = q;
            ViewBag.CurrentPage = page; // I-save para sa UI sa ubos sa page

            // Pila ka produkto ang i-display kada loading? (12 o 16 kay nindot sa grid)
            int pageSize = 12;

            // 1. Paggamit og .AsNoTracking() — Kani makapagaan kaayo sa memory sa server!
            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category);
            // GI-TANGTANG UNA ANG PRODUCTVARIANTS: Ayaw i-include ang variants sa Home page 
            // gawas lang kung gikinahanglan gyud nimo ang variant price sa preview card.

            // 2. SEARCH & FILTER LOGIC
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(p => p.Name.Contains(q) || p.Category.Name.Contains(q));
                ViewBag.CategoryName = "Resulta para sa \"" + q + "\"";
            }
            else if (id.HasValue)
            {
                query = query.Where(p => p.CategoryId == id.Value);
                var category = await _context.Categories.FindAsync(id.Value);
                ViewBag.CategoryName = category?.Name ?? "Items";
            }
            else
            {
                ViewBag.CategoryName = "Daily Curated Items";
            }

            // 3. PAGINATION LOGIC (Skip ug Take) — Kani ang sekreto aron paspas kaayo sa device!
            var products = await query
                .OrderByDescending(p => p.ProductId) // I-una ang pinakabag-o nga na-upload
                .Skip((page - 1) * pageSize)         // Laktawan ang mga nangaging pages
                .Take(pageSize)                      // Kuhaa lang ang tag-12 ka buok
                .ToListAsync();

            // Pwede sab ka mag-pasa og Total Pages sa ViewBag para sa imong "Next/Previous" buttons:
            int totalItems = await query.CountAsync();
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);





            var userIdString = (int)Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

          
            
            

           
                // 2. Pangitaon ang pinakabag-o nga order sa maong user gikan sa imong Database Context
                // Gi-sort nato gamit ang OrderId o DateCreated descending para ang pinaka-latest gyud ang makuha
                var latestActualOrder = _context.Orders
                    .Where(o => o.UserId == userIdString|| o.User.UserId == userIdString)
                    .OrderByDescending(o => o.OrderDate) // o o.OrderId
                    .FirstOrDefault();

                if (latestActualOrder != null)
                {
                    // 3. I-pasa ang tinuod nga status (e.g., "Confirmed", "Shipping") ngadto sa ViewBag
                    ViewBag.CurrentOrderStatus = latestActualOrder.Status;
                }
                else
                {
                    // Kung log-in ang user pero wala pa siya'y bisan unsang order sa kasaysayan sa B-Hub
                    ViewBag.CurrentOrderStatus = "Pending";
                
            }


            

            return View("~/Pages/Public/Home.cshtml", products);

        }
    }
}