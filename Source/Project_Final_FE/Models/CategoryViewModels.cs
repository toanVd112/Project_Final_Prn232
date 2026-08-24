using System.ComponentModel.DataAnnotations;

namespace Project_Final_FE.Models
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BookCount { get; set; }
    }

    public class CategoryFormViewModel
    {
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Tên thể loại không được để trống.")]
        [MaxLength(100, ErrorMessage = "Tên thể loại tối đa 100 ký tự.")]
        [Display(Name = "Tên thể loại")]
        public string Name { get; set; } = string.Empty;
    }
}
