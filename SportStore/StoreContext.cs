using Microsoft.EntityFrameworkCore;
using SportStore.Models;

namespace SportStore
{
    public class StoreContext : DbContext
    {
        public StoreContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Khởi tạo dữ liệu mặc định
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Password = "admin", Email = "admin@example.com", IsAdmin = true },
                new User { Id = 2, Username = "c", Password = "c", Email = "c@example.com", IsAdmin = false }
            );
        }
    }
}