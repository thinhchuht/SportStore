using Microsoft.AspNetCore.Mvc;
using SportStore.Models;

namespace SportStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController(StoreContext context, IWebHostEnvironment environment) : Controller
    {
        private readonly string _imagePath = Path.Combine("wwwroot", "images", "products");

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            var products = context.Products.ToList();
            return View(products);
        }

        // GET: Thêm/sửa sản phẩm
        public IActionResult Edit(int? id)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            var product = id.HasValue ? context.Products.Find(id) : new Product();
            var viewModel = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Quantity = product.Quantity,
                Image = product.Image,
                Description = product.Description
            };
            return View(viewModel);
        }

        // POST: Lưu sản phẩm
        [HttpPost]
        public async Task<IActionResult> Edit(ProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra file upload
                if (viewModel.Id == 0 && (viewModel.ImageFile == null || viewModel.ImageFile.Length == 0))
                {
                    ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh sản phẩm");
                    return View(viewModel);
                }
                string image = viewModel.Image; // Keep existing image if no new file is uploaded
                if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
                {
                    // Kiểm tra định dạng file
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(viewModel.ImageFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif)");
                        return View(viewModel);
                    }

                    // Kiểm tra kích thước file (tối đa 5MB)
                    if (viewModel.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB");
                        return View(viewModel);
                    }

                    // Tạo thư mục nếu chưa tồn tại
                    var uploadPath = Path.Combine(environment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Tạo tên file duy nhất
                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadPath, fileName);

                    // Lưu file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.ImageFile.CopyToAsync(stream);
                    }

                    // Cập nhật đường dẫn ảnh
                    image = $"/images/products/{fileName}";
                }

                var product = viewModel.Id == 0 ? new Product() : context.Products.Find(viewModel.Id);
                if (product == null)
                {
                    return NotFound();
                }

                product.Name = viewModel.Name;
                product.Price = viewModel.Price;
                product.Quantity = viewModel.Quantity;
                if (!string.IsNullOrEmpty(image))
                {
                    product.Image = image;
                }
                product.Description = viewModel.Description;

                if (viewModel.Id == 0)
                {
                    context.Products.Add(product);
                }
                else
                {
                    context.Products.Update(product);
                }
                context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // Xóa sản phẩm
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var product = context.Products.Find(id);
            if (product != null)
            {
                // Xóa file ảnh nếu tồn tại
                if (!string.IsNullOrEmpty(product.Image))
                {
                    var imagePath = Path.Combine(environment.WebRootPath, product.Image.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                context.Products.Remove(product);
                context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}