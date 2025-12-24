using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
        
        public string SessionId { get; set; } = string.Empty;
        
        public string? UserId { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}