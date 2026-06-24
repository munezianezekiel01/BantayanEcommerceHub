using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;

namespace MyEccomerce.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Route("Home/Category/{id}")]
        public async Task<IActionResult> Category(int id)
        {
            // 1. Kuhaa ang main category lakip ang iyang mga sub-categories (anak)
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return RedirectToAction("Home", "Home");
            }

            // 2. Paghimo og listahan sa tanang IDs (ang Parent ID + tanang Sub-category IDs)
            var categoryIds = new List<int> { id };
            if (category.SubCategories != null && category.SubCategories.Any())
            {
                categoryIds.AddRange(category.SubCategories.Select(s => s.CategoryId));
            }

            // 3. I-filter ang products base sa maong listahan sa IDs
            var products = await _context.Products
                .Where(p => categoryIds.Contains(p.CategoryId))
                .ToListAsync();

            // 4. I-pasa ang data sa View
            ViewBag.CategoryName = category.Name;
            ViewBag.CategoryIcon = category.IconClass;

            // Temporary check para sa user (ilisi lang ni sa imong actual auth logic)
            ViewBag.CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == 1);

            return View("~/Pages/Public/Category.cshtml", products);
        }
    }
}