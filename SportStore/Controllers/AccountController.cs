using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using SportStore.Models;

namespace SportStore.Controllers
{
    public class AccountController(StoreContext context, IConfiguration configuration) : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetInt32("IsAdmin", user.IsAdmin ? 1 : 0);
                return user.IsAdmin ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Home");
            }
            ViewBag.Message = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (context.Users.Any(u => u.Username == user.Username))
            {
                ViewBag.Message = "Tên đăng nhập đã tồn tại!";
                return View();
            }
            if (context.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Message = "Email đã được sử dụng!";
                return View();
            }

            // Tạo mã xác nhận ngẫu nhiên 6 chữ số
            Random random = new Random();
            string verificationCode = random.Next(100000, 999999).ToString();
            TempData["VerificationCode"] = verificationCode;
            TempData["PendingUser"] = System.Text.Json.JsonSerializer.Serialize(user);

            // Gửi email
            SendVerificationEmail(user.Email, verificationCode);

            return RedirectToAction("VerifyCode");
        }

        public IActionResult VerifyCode()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyCode(string code)
        {
            var expectedCode = TempData["VerificationCode"]?.ToString();
            if (code == expectedCode)
            {
                var pendingUserJson = TempData["PendingUser"]?.ToString();
                if (pendingUserJson != null)
                {
                    var user = System.Text.Json.JsonSerializer.Deserialize<User>(pendingUserJson);
                    context.Users.Add(user);
                    context.SaveChanges();
                    TempData["Message"] = "Đăng ký thành công!";
                    return RedirectToAction("Login");
                }
            }
            ViewBag.Message = "Mã xác nhận không đúng!";
            TempData.Keep("VerificationCode"); // Giữ mã để thử lại
            TempData.Keep("PendingUser");
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private void SendVerificationEmail(string email, string code)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sport Store", "thinhchuht0@gmail.com"));
            message.To.Add(new MailboxAddress("KH", email));
            message.Subject = "Mã xác nhận đăng ký";
            message.Body = new TextPart("plain")
            {
                Text = $"Mã xác nhận của bạn là: {code}"
            };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                client.Authenticate("thinhchuht0@gmail.com", "sbpe apta ksal uygf");
                client.Send(message);
                client.Disconnect(true);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email");
                return View();
            }

            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống");
                return View();
            }

            // Tạo mật khẩu mới ngẫu nhiên
            var newPassword = GenerateRandomPassword();
            
            // Cập nhật mật khẩu mới
            user.Password = newPassword;
            await context.SaveChangesAsync();

            // Gửi email chứa mật khẩu mới
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sport Store", "thinhchuht0@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Đặt lại mật khẩu";
            message.Body = new TextPart("plain")
            {
                Text = $"Mật khẩu mới của bạn là: {newPassword}\nVui lòng đăng nhập và đổi mật khẩu ngay sau khi nhận được email này."
            };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                client.Authenticate("thinhchuht0@gmail.com", "sbpe apta ksal uygf");
                client.Send(message);
                client.Disconnect(true);
            }

            TempData["Message"] = "Mật khẩu mới đã được gửi đến email của bạn";
            return RedirectToAction("Login");
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
