using System.ComponentModel.DataAnnotations;

namespace SportStore.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên danh mục không được vượt quá 50 ký tự")]
        public string Name { get; set; }

        public string Description { get; set; }
        public List<Product> Products { get; set; }
    }
} 