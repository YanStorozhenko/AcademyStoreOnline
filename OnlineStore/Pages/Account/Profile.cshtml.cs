using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        
        public ProfileModel(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        
        [BindProperty]
        public string Email { get; set; } = string.Empty;
        
        [BindProperty]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [BindProperty]
        public string Address { get; set; } = string.Empty;
        
        [BindProperty]
        public string City { get; set; } = string.Empty;
        
        [BindProperty]
        public string PostalCode { get; set; } = string.Empty;
        
        [BindProperty]
        public string Country { get; set; } = string.Empty;
        
        public DateTime? RegistrationDate { get; set; }
        
        public List<Order> Orders { get; set; } = new List<Order>();
        
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }
            
            await LoadUserData(user);
            await LoadOrders(user.Id);
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }
            
            // Обновляем данные пользователя
            user.Email = Email;
            user.PhoneNumber = PhoneNumber;
            
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Профиль успешно обновлен!";
            }
            else
            {
                TempData["Error"] = "Ошибка при обновлении профиля";
            }
            
            await LoadUserData(user);
            await LoadOrders(user.Id);
            
            return Page();
        }
        
        private Task LoadUserData(IdentityUser user)
        {
            Email = user.Email ?? string.Empty;
            PhoneNumber = user.PhoneNumber ?? string.Empty;
            RegistrationDate = user.EmailConfirmed ? DateTime.UtcNow : DateTime.UtcNow.AddDays(-30);
            
            return Task.CompletedTask;
        }
        
        private async Task LoadOrders(string userId)
        {
            Orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}