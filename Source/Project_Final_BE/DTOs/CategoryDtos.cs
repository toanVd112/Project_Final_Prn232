using System.ComponentModel.DataAnnotations;

namespace Project_Final_BE.DTOs
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BookCount { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Tên thể loại không được để trống.")]
        [MaxLength(100, ErrorMessage = "Tên thể loại tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Tên thể loại không được để trống.")]
        [MaxLength(100, ErrorMessage = "Tên thể loại tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;
    }
}
