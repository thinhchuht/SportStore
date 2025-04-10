using System.ComponentModel.DataAnnotations;

namespace SportStore.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string OrderNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = "pending"; // pending, processing, completed, cancelled
        public string PaymentStatus { get; set; } = "pending"; // pending, paid, failed
        public string PaymentMethod { get; set; } = "cod"; // cod, bank_transfer, etc.
        public string ShippingAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public User User { get; set; }
        public List<OrderItem> OrderItems { get; set; }
    }
} 