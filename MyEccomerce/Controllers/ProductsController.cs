using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Models;
using System.Security.Claims;

namespace MyEccomerce.Controllers
{

    public class ProductsController : Controller
    {
        public readonly ApplicationDbContext _context;


        public ProductsController(ApplicationDbContext context)
        {

            _context = context;
        }


        [Route("Product/Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            // 1. Paggamit og .AsNoTracking() — Makapagaan kaayo sa query performance sa server
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.ProductVariants)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            product.ViewCount += 1;
            _context.Products.Update(product); // Siguradoha nga e-update ang product tracker
            await _context.SaveChangesAsync();

            // 2. KUHAON ANG USER PARA SA LAYOUT (Gi-optimize ug gi-AsNoTracking sab)
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email))
            {
                // Gilahi nato ang pag-query ug gigamitan og AsNoTracking para paspas
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == email);

                ViewBag.CurrentUser = user;
                ViewBag.CurrentUserId = user.UserId;
            }

            // Siguradoha nga dili null ang UnitName (Fallback value)
            if (string.IsNullOrEmpty(product.UnitName))
            {
                product.UnitName = "pc";
            }

            return View("~/Pages/Public/Details.cshtml", product);
        }

        [HttpGet]


        [HttpGet]
        [Route("Products/GetSearchSuggestions")]
        [HttpGet]
        public async Task<IActionResult> GetSearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new List<object>());

            var suggestions = await _context.Products
                .Where(p => p.Name.StartsWith(term) || p.Name.Contains(term)) // Gi-una ang StartsWith para sa accuracy
                .OrderBy(p => p.Name.StartsWith(term) ? 0 : 1) // I-una og pakita ang nag-start jud sa maong letra
                .Take(5) // Limitahi lang para paspas ang response
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.Name,
                    price = p.Price,
                    img = p.ImageUrl
                })
                .ToListAsync();

            return Json(suggestions);
        }

        [HttpPost]
        [Route("Product/SaveTimeSpent")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SaveTimeSpent()
        {
            try
            {
                // Mas luwas ug sigurado kon mano-mano natong kuhaon gikan sa Form request data
                var requestForm = Request.Form;

                string authUserId = requestForm["authUserId"];
                string guestSessionId = requestForm["guestSessionId"];

                int.TryParse(requestForm["productId"], out int productId);
                int.TryParse(requestForm["seconds"], out int seconds);

                // 1. I-log kon unsa gyuy sulod sa nadawat
                if (productId == 0 || seconds == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Kulang o sipyat ang parsing! ProductId={productId}, Seconds={seconds}"
                    });
                }

                if (seconds < 2)
                {
                    return BadRequest(new { success = false, message = $"Gi-block kay {seconds}s ra." });
                }

                string finalUserId = !string.IsNullOrEmpty(authUserId) ? authUserId : guestSessionId;

                var viewLog = new ProductViewLog
                {
                    UserId = finalUserId,
                    ProductId = productId,
                    SecondsSpent = seconds,
                    ViewDateTime = DateTime.Now
                };

                _context.productViewLogs.Add(viewLog);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                string errorText = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { success = false, message = errorText });
            }
        }
    }
}