using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportStore.Models;

namespace SportStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly StoreContext _context;

        public OrderController(StoreContext context)
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

            var orders = _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return View(orders);
        }

        public IActionResult Details(int id)
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

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return RedirectToAction("Index");
            }

            return View(order);
        }

        [HttpPost]
        public IActionResult CancelOrder(int id)
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

            var order = _context.Orders
                .FirstOrDefault(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return RedirectToAction("Index");
            }

            if (order.OrderStatus == "pending" || order.OrderStatus == "processing")
            {
                order.OrderStatus = "cancelled";
                _context.SaveChanges();

                // Hoàn trả số lượng sản phẩm
                var orderItems = _context.OrderItems
                    .Where(oi => oi.OrderId == order.Id)
                    .ToList();

                foreach (var item in orderItems)
                {
                    var product = _context.Products.Find(item.ProductId);
                    if (product != null)
                    {
                        product.Quantity += item.Quantity;
                        _context.Products.Update(product);
                    }
                }

                _context.SaveChanges();
            }

            return RedirectToAction("Details", new { id = order.Id });
        }
    }
}