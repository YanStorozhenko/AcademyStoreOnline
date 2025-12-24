using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using System.Security.Claims;

namespace OnlineStore.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        
        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        // Для дашборда
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        
        // Для управления товарами
        public List<Product> Products { get; set; } = new List<Product>();
        
        // Для управления категориями
        public List<Category> Categories { get; set; } = new List<Category>();
        public int? EditingCategoryId { get; set; }
        public string? EditingCategoryName { get; set; }
        
        // Для управления заказами
        public List<Order> AllOrders { get; set; } = new List<Order>();
        
        // Для управления пользователями
        public List<UserInfo> Users { get; set; } = new List<UserInfo>();
        
        // Активный раздел
        [BindProperty(SupportsGet = true)]
        public string ActiveSection { get; set; } = "dashboard";
        
        // Параметры для редактирования категории
        [BindProperty(SupportsGet = true)]
        public int? Edit { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? Name { get; set; }
        
        public class UserInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public DateTime RegistrationDate { get; set; }
            public int OrderCount { get; set; }
        }
        
        public async Task OnGetAsync()
        {
            ActiveSection = string.IsNullOrEmpty(ActiveSection) ? "dashboard" : ActiveSection;
            
            switch (ActiveSection)
            {
                case "dashboard":
                    await LoadDashboardData();
                    break;
                    
                case "products":
                    await LoadProductsData();
                    break;
                    
                case "categories":
                    await LoadCategoriesData();
                    break;
                    
                case "orders":
                    await LoadOrdersData();
                    break;
                    
                case "users":
                    await LoadUsersData();
                    break;
            }
        }
        
        public async Task<IActionResult> OnPostDeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product != null)
                {
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Товар успешно удален";
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Ошибка при удалении товара";
            }
            
            return RedirectToPage(new { section = "products" });
        }
        
        public async Task<IActionResult> OnPostSaveCategoryAsync(int? id, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Error"] = "Название категории не может быть пустым";
                    return RedirectToPage(new { section = "categories" });
                }
                
                if (id.HasValue)
                {
                    // Редактирование существующей категории
                    var category = await _context.Categories.FindAsync(id.Value);
                    if (category != null)
                    {
                        category.Name = name;
                        _context.Categories.Update(category);
                    }
                }
                else
                {
                    // Создание новой категории
                    var category = new Category { Name = name };
                    _context.Categories.Add(category);
                }
                
                await _context.SaveChangesAsync();
                TempData["Success"] = id.HasValue ? "Категория обновлена" : "Категория добавлена";
            }
            catch (Exception)
            {
                TempData["Error"] = "Ошибка при сохранении категории";
            }
            
            return RedirectToPage(new { section = "categories" });
        }
        
        public async Task<IActionResult> OnPostDeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category != null)
                {
                    _context.Categories.Remove(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Категория удалена";
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Ошибка при удалении категории";
            }
            
            return RedirectToPage(new { section = "categories" });
        }
        
        public async Task<IActionResult> OnPostUpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.Status = status;
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки
            }
            
            return RedirectToPage(new { section = "orders" });
        }
        
        public async Task<IActionResult> OnPostToggleAdminAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Admin");
                        TempData["Success"] = "Права администратора удалены";
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                        TempData["Success"] = "Права администратора добавлены";
                    }
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Ошибка при изменении прав";
            }
            
            return RedirectToPage(new { section = "users" });
        }
        
        private async Task LoadDashboardData()
        {
            TotalProducts = await _context.Products.CountAsync();
            TotalOrders = await _context.Orders.CountAsync();
            TotalUsers = await _userManager.Users.CountAsync();
            TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            RecentOrders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();
        }
        
        private async Task LoadProductsData()
        {
            Products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
        
        private async Task LoadCategoriesData()
        {
            Categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
            
            if (Edit.HasValue && !string.IsNullOrEmpty(Name))
            {
                EditingCategoryId = Edit.Value;
                EditingCategoryName = Name;
            }
        }
        
        private async Task LoadOrdersData()
        {
            AllOrders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        
        private async Task LoadUsersData()
        {
            var allUsers = await _userManager.Users.ToListAsync();
            
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var orderCount = await _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .CountAsync();
                
                Users.Add(new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "User",
                    RegistrationDate = user.EmailConfirmed ? DateTime.UtcNow : DateTime.UtcNow.AddDays(-30),
                    OrderCount = orderCount
                });
            }
            
            Users = Users.OrderByDescending(u => u.Role).ThenBy(u => u.Email).ToList();
        }
    }
}