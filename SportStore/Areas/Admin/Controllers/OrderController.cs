using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportStore.Models;
using System.Collections.Generic;
using System.Linq;

namespace SportStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly StoreContext _context;

        public OrderController(StoreContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var orders = _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return View(orders);
        }

        public IActionResult Details(int id)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return RedirectToAction("Index");
            }

            // Kiểm tra OrderItems
            if (order.OrderItems == null)
            {
                // Nếu OrderItems là null, khởi tạo danh sách trống
                order.OrderItems = new List<OrderItem>();
                TempData["Error"] = "Không tìm thấy thông tin chi tiết đơn hàng";
            }
            else if (!order.OrderItems.Any())
            {
                // Nếu không có sản phẩm, thử tải trực tiếp từ database
                var orderItems = _context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == id)
                    .ToList();

                if (orderItems.Any())
                {
                    order.OrderItems = orderItems;
                }
                else
                {
                    TempData["Error"] = "Đơn hàng không có sản phẩm nào";
                }
            }

            // Debug information
            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"];
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"];
            ViewBag.AreaName = ControllerContext.RouteData.Values["area"];
            ViewBag.OrderItemsCount = order.OrderItems?.Count ?? 0;

            // Explicitly specify the view path
            return View("~/Areas/Admin/Views/Order/Details.cshtml", order);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string orderStatus, string paymentStatus)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var order = _context.Orders.Find(id);

            if (order == null)
            {
                return RedirectToAction("Index");
            }

            // Nếu đơn hàng đang bị hủy và được cập nhật thành trạng thái khác
            if (order.OrderStatus == "cancelled" && orderStatus != "cancelled")
            {
                // Kiểm tra số lượng sản phẩm
                var orderItems = _context.OrderItems
                    .Where(oi => oi.OrderId == order.Id)
                    .Include(oi => oi.Product)
                    .ToList();

                foreach (var item in orderItems)
                {
                    if (item.Product.Quantity < item.Quantity)
                    {
                        TempData["Error"] = $"Không thể cập nhật trạng thái đơn hàng. Sản phẩm '{item.Product.Name}' không đủ số lượng.";
                        return RedirectToAction("Details", new { id = order.Id });
                    }
                }

                // Cập nhật số lượng sản phẩm
                foreach (var item in orderItems)
                {
                    item.Product.Quantity -= item.Quantity;
                    _context.Products.Update(item.Product);
                }
            }
            // Nếu đơn hàng đang ở trạng thái khác và được cập nhật thành hủy
            else if (order.OrderStatus != "cancelled" && orderStatus == "cancelled")
            {
                // Hoàn trả số lượng sản phẩm
                var orderItems = _context.OrderItems
                    .Where(oi => oi.OrderId == order.Id)
                    .Include(oi => oi.Product)
                    .ToList();

                foreach (var item in orderItems)
                {
                    item.Product.Quantity += item.Quantity;
                    _context.Products.Update(item.Product);
                }
            }

            order.OrderStatus = orderStatus;
            order.PaymentStatus = paymentStatus;
            _context.SaveChanges();

            TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công";
            return RedirectToAction("Details", new { id = order.Id });
        }
    }
}