using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportStore.Models;
using System.Text;

namespace SportStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly StoreContext _context;

        public CheckoutController(StoreContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Login", "Account");
            }

            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToList();

            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.TotalAmount = cartItems.Sum(c => c.Price * c.Quantity);
            ViewBag.CartItems = cartItems;

            return View();
        }

        [HttpPost]
        public IActionResult PlaceOrder(string shippingAddress, string phoneNumber, string paymentMethod, string notes)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Login", "Account");
            }

            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToList();

            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // Kiểm tra số lượng sản phẩm
            foreach (var item in cartItems)
            {
                if (item.Product.Quantity < item.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm '{item.Product.Name}' chỉ còn {item.Product.Quantity} sản phẩm";
                    return RedirectToAction("Index");
                }
            }

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = user.Id,
                OrderNumber = GenerateOrderNumber(),
                TotalAmount = cartItems.Sum(c => c.Price * c.Quantity),
                OrderStatus = "pending",
                PaymentStatus = "pending",
                PaymentMethod = paymentMethod,
                ShippingAddress = shippingAddress,
                PhoneNumber = phoneNumber,
                Notes = notes,
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            // Tạo chi tiết đơn hàng
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    CreatedAt = DateTime.Now
                };

                _context.OrderItems.Add(orderItem);

                // Cập nhật số lượng sản phẩm
                var product = _context.Products.Find(item.ProductId);
                product.Quantity -= item.Quantity;
                _context.Products.Update(product);
            }

            // Xóa giỏ hàng
            _context.Carts.RemoveRange(cartItems);
            _context.SaveChanges();

            return RedirectToAction("OrderSuccess", new { orderId = order.Id });
        }

        public IActionResult OrderSuccess(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        private string GenerateOrderNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random();
            var randomNumber = random.Next(1000, 9999);
            return $"ORD-{timestamp}-{randomNumber}";
        }
    }
}