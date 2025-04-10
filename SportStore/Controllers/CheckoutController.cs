using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportStore.Models;
using System.Text;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace SportStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly StoreContext _context;
        private readonly IConfiguration _configuration;

        public CheckoutController(StoreContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

            // Validate input
            if (string.IsNullOrWhiteSpace(shippingAddress) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin giao hàng";
                return RedirectToAction("Index");
            }

            // Validate phone number
            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\d{10,11}$"))
            {
                TempData["Error"] = "Số điện thoại không hợp lệ";
                return RedirectToAction("Index");
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
                TempData["Error"] = "Giỏ hàng của bạn đang trống";
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

            // Sử dụng transaction để đảm bảo tính nhất quán dữ liệu
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Tạo đơn hàng
                    var order = new Order
                    {
                        UserId = user.Id,
                        OrderNumber = GenerateOrderNumber(),
                        TotalAmount = cartItems.Sum(c => c.Price * c.Quantity),
                        OrderStatus = "pending",
                        PaymentStatus = paymentMethod == "cod" ? "pending" : "awaiting_payment",
                        PaymentMethod = paymentMethod,
                        ShippingAddress = shippingAddress,
                        PhoneNumber = phoneNumber,
                        Notes = notes,
                        CreatedAt = DateTime.Now
                    };

                    _context.Orders.Add(order);
                    _context.SaveChanges();

                    // Tạo chi tiết đơn hàng và cập nhật số lượng sản phẩm
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
                        if (product != null)
                        {
                            product.Quantity -= item.Quantity;
                            _context.Products.Update(product);
                        }
                    }

                    // Xóa giỏ hàng
                    _context.Carts.RemoveRange(cartItems);
                    _context.SaveChanges();

                    // Commit transaction
                    transaction.Commit();

                    return RedirectToAction("OrderSuccess", new { orderId = order.Id });
                }
                catch (Exception ex)
                {
                    // Rollback transaction nếu có lỗi
                    transaction.Rollback();
                    TempData["Error"] = "Đã xảy ra lỗi khi đặt hàng: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        public IActionResult OrderSuccess(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Send confirmation email
            SendOrderConfirmationEmail(order);

            return View(order);
        }

        private void SendOrderConfirmationEmail(Order order)
        {
            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MailboxAddress("Sport Store", "thinhchuht0@gmail.com"));
                message.To.Add(new MailboxAddress("", order.User.Email));
                message.Subject = $"Xác nhận đơn hàng #{order.OrderNumber}";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.TextBody = BuildOrderEmailContent(order);
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    client.Authenticate("thinhchuht0@gmail.com", "sbpe apta ksal uygf");
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't stop the order process
                Console.WriteLine($"Failed to send order confirmation email: {ex.Message}");
            }
        }

        private string BuildOrderEmailContent(Order order)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Xin chào {order.User.Username},");
            sb.AppendLine();
            sb.AppendLine($"Cảm ơn bạn đã đặt hàng tại Sport Store. Đơn hàng của bạn đã được xác nhận.");
            sb.AppendLine();
            sb.AppendLine($"Thông tin đơn hàng #{order.OrderNumber}:");
            sb.AppendLine($"Ngày đặt hàng: {order.CreatedAt.ToString("dd/MM/yyyy HH:mm")}");
            sb.AppendLine($"Trạng thái đơn hàng: {order.OrderStatus}");
            sb.AppendLine($"Phương thức thanh toán: {order.PaymentMethod}");
            sb.AppendLine($"Trạng thái thanh toán: {order.PaymentStatus}");
            sb.AppendLine($"Địa chỉ giao hàng: {order.ShippingAddress}");
            sb.AppendLine($"Số điện thoại: {order.PhoneNumber}");
            sb.AppendLine();

            sb.AppendLine("Chi tiết đơn hàng:");
            sb.AppendLine("--------------------------------------------------");
            foreach (var item in order.OrderItems)
            {
                sb.AppendLine($"{item.Product.Name} x {item.Quantity} = {item.Price * item.Quantity:N0} VNĐ");
            }
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"Tổng tiền: {order.TotalAmount:N0} VNĐ");
            sb.AppendLine();
            sb.AppendLine("Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.");
            sb.AppendLine();
            sb.AppendLine("Trân trọng,");
            sb.AppendLine("Sport Store");

            return sb.ToString();
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