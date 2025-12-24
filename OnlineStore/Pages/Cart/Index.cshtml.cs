using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Pages.Cart
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        
        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal TotalAmount { get; set; }
        
        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCartItems();
            return Page();
        }
        
        public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity = 1)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null || product.Stock < quantity)
                {
                    TempData["Error"] = "Товар не найден или нет в нужном количестве";
                    return RedirectToPage("/Catalog/Index");
                }
                
                var userId = _userManager.GetUserId(User);
                var sessionId = GetSessionId();
                
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => 
                        ci.ProductId == productId && 
                        (ci.UserId == userId || ci.SessionId == sessionId));
                
                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        UserId = userId,
                        SessionId = sessionId,
                        AddedAt = DateTime.UtcNow
                    };
                    
                    _context.CartItems.Add(cartItem);
                }
                
                await _context.SaveChangesAsync();
                TempData["Success"] = "Товар добавлен в корзину!";
            }
            catch
            {
                TempData["Error"] = "Ошибка при добавлении товара в корзину";
            }
            
            return RedirectToPage("/Catalog/Index");
        }
        
        public async Task<IActionResult> OnPostUpdateQuantityAsync(int cartItemId, int change)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
                
                if (cartItem != null && cartItem.Product != null)
                {
                    cartItem.Quantity += change;
                    
                    if (cartItem.Quantity < 1)
                    {
                        cartItem.Quantity = 1;
                    }
                    
                    if (cartItem.Quantity > 10)
                    {
                        cartItem.Quantity = 10;
                    }
                    
                    if (cartItem.Product.Stock < cartItem.Quantity)
                    {
                        TempData["Error"] = "Недостаточно товара на складе";
                        return RedirectToPage();
                    }
                    
                    _context.CartItems.Update(cartItem);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                TempData["Error"] = "Ошибка при обновлении количества";
            }
            
            return RedirectToPage();
        }
        
        public async Task<IActionResult> OnPostRemoveItemAsync(int cartItemId)
        {
            try
            {
                var cartItem = await _context.CartItems.FindAsync(cartItemId);
                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Товар удален из корзины";
                }
            }
            catch
            {
                TempData["Error"] = "Ошибка при удалении товара";
            }
            
            return RedirectToPage();
        }
        
        public async Task<IActionResult> OnPostClearCartAsync()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var sessionId = GetSessionId();
                
                var itemsToRemove = await _context.CartItems
                    .Where(ci => ci.UserId == userId || ci.SessionId == sessionId)
                    .ToListAsync();
                
                if (itemsToRemove.Any())
                {
                    _context.CartItems.RemoveRange(itemsToRemove);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Корзина очищена";
                }
            }
            catch
            {
                TempData["Error"] = "Ошибка при очистке корзины";
            }
            
            return RedirectToPage();
        }
        
        public async Task<IActionResult> OnPostCheckoutAsync()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.UserId == userId)
                    .ToListAsync();
                
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Корзина пуста";
                    return RedirectToPage();
                }
                
                // Проверяем наличие товаров
                foreach (var item in cartItems)
                {
                    if (item.Product == null || item.Product.Stock < item.Quantity)
                    {
                        TempData["Error"] = $"Товар '{item.Product?.Name}' недоступен в нужном количестве";
                        return RedirectToPage();
                    }
                }
                
                // Создаем заказ
                var order = new Order
                {
                    UserId = userId!,
                    OrderDate = DateTime.UtcNow,
                    Status = "Ожидает оплаты",
                    TotalAmount = cartItems.Sum(ci => ci.Product!.Price * ci.Quantity)
                };
                
                foreach (var cartItem in cartItems)
                {
                    if (cartItem.Product != null)
                    {
                        var orderItem = new OrderItem
                        {
                            ProductId = cartItem.ProductId,
                            Quantity = cartItem.Quantity,
                            Price = cartItem.Product.Price
                        };
                        
                        order.Items.Add(orderItem);
                        
                        // Уменьшаем количество на складе
                        cartItem.Product.Stock -= cartItem.Quantity;
                        _context.Products.Update(cartItem.Product);
                    }
                }
                
                _context.Orders.Add(order);
                
                // Удаляем товары из корзины
                _context.CartItems.RemoveRange(cartItems);
                
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Заказ №{order.Id} успешно оформлен! Сумма: {order.TotalAmount:C}";
                return RedirectToPage("/Account/Profile");
            }
            catch
            {
                TempData["Error"] = "Ошибка при оформлении заказа";
                return RedirectToPage();
            }
        }
        
        public async Task<IActionResult> OnGetGetCountAsync()
        {
            var userId = _userManager.GetUserId(User);
            var sessionId = GetSessionId();
            
            var count = await _context.CartItems
                .Where(ci => ci.UserId == userId || ci.SessionId == sessionId)
                .SumAsync(ci => ci.Quantity);
            
            return Content(count.ToString());
        }
        
        private async Task LoadCartItems()
        {
            var userId = _userManager.GetUserId(User);
            var sessionId = GetSessionId();
            
            CartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .ThenInclude(p => p.Category)
                .Where(ci => ci.UserId == userId || ci.SessionId == sessionId)
                .OrderByDescending(ci => ci.AddedAt)
                .ToListAsync();
            
            TotalAmount = CartItems.Sum(ci => (ci.Product?.Price ?? 0) * ci.Quantity);
        }
        
        private string GetSessionId()
        {
            var sessionId = HttpContext.Session.GetString("CartSessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("CartSessionId", sessionId);
            }
            return sessionId;
        }
    }
}