namespace SportStore.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // Lưu ý: Trong thực tế nên băm mật khẩu
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}