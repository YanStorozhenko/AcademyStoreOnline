using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        
        public CartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        [HttpGet("count")]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var sessionId = GetSessionId();
                
                var count = await _context.CartItems
                    .Where(ci => ci.UserId == userId || ci.SessionId == sessionId)
                    .SumAsync(ci => ci.Quantity);
                
                return Ok(count);
            }
            catch
            {
                return Ok(0);
            }
        }
        
        [HttpPost("add/{productId}")]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, [FromQuery] int quantity = 1)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null || product.Stock < quantity)
                {
                    return BadRequest(new { message = "Товар не найден или нет в нужном количестве" });
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
                    _context.CartItems.Update(existingCartItem);
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
                
                var newCount = await _context.CartItems
                    .Where(ci => ci.UserId == userId || ci.SessionId == sessionId)
                    .SumAsync(ci => ci.Quantity);
                
                return Ok(new { 
                    success = true, 
                    message = "Товар добавлен в корзину",
                    count = newCount 
                });
            }
            catch
            {
                return BadRequest(new { message = "Ошибка при добавлении товара в корзину" });
            }
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