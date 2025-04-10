using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportStore.Models;

namespace SportStore.Controllers
{
    public class CartController : Controller
    {
        private readonly StoreContext _context;

        public CartController(StoreContext context)
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

            return View(cartItems);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng" });
            }

            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            }

            var product = _context.Products.Find(productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            if (product.Quantity < quantity)
            {
                return Json(new { success = false, message = "Số lượng sản phẩm không đủ" });
            }

            var cartItem = _context.Carts
                .FirstOrDefault(c => c.UserId == user.Id && c.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new Cart
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    Price = product.Price
                };
                _context.Carts.Add(cartItem);
            }

            _context.SaveChanges();

            return Json(new { success = true, message = "Sản phẩm đã được thêm vào giỏ hàng" });
        }

        [HttpPost]
        public IActionResult UpdateCart(int cartId, int quantity)
        {
            var cartItem = _context.Carts.Find(cartId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }

            var product = _context.Products.Find(cartItem.ProductId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            if (quantity <= 0)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
                return Json(new { success = true, message = "Sản phẩm đã được xóa khỏi giỏ hàng" });
            }

            if (product.Quantity < quantity)
            {
                return Json(new { success = false, message = "Số lượng sản phẩm không đủ" });
            }

            cartItem.Quantity = quantity;
            _context.SaveChanges();

            return Json(new { success = true, message = "Giỏ hàng đã được cập nhật" });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int cartId)
        {
            var cartItem = _context.Carts.Find(cartId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }

            _context.Carts.Remove(cartItem);
            _context.SaveChanges();

            return Json(new { success = true, message = "Sản phẩm đã được xóa khỏi giỏ hàng" });
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            }

            var cartItems = _context.Carts.Where(c => c.UserId == user.Id);
            _context.Carts.RemoveRange(cartItems);
            _context.SaveChanges();

            return Json(new { success = true, message = "Giỏ hàng đã được xóa" });
        }
    }
}