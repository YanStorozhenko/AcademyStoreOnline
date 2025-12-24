using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        
        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
        
        public async Task OnGetAsync()
        {
            // Получаем популярные товары (те, что с самым большим остатком на складе)
            Products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Stock > 0)
                .OrderByDescending(p => p.Stock)
                .Take(8)
                .ToListAsync();
            
            // Получаем все категории
            Categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}