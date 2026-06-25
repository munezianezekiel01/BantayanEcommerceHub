using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Models;

namespace MyEccomerce.Pages.Public
{
    // USBA NI GIKAN SA IndexModel -> HomeModel
    public class HomeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public HomeModel(ApplicationDbContext context)
        {
            _context = context;
        }


        public List<Product> Products { get; set; } = new();
        public string SelectedCategoryName { get; set; } = "Daily Curated Items";

        public async Task OnGetAsync(int? id)
        {
            Products = await _context.Products.ToListAsync();

            // I-pasa ang data pinaagi sa ViewData para mabasa sa Layout
            
        }
    }
}