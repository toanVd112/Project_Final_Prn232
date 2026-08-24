using System.ComponentModel.DataAnnotations;

namespace Project_Final_BE.DTOs
{
    public class BookDto
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
    }

    public class CreateBookDto
    {
        [Required(ErrorMessage = "Tên sách không được để trống.")]
        [MaxLength(200, ErrorMessage = "Tên sách tối đa 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tác giả không được để trống.")]
        [MaxLength(100, ErrorMessage = "Tác giả tối đa 100 ký tự.")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá bìa không được để trống.")]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá bìa phải lớn hơn 1.000 VNĐ.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Thể loại không được để trống.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tổng số bản in không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Tổng số bản in phải lớn hơn 0.")]
        public int TotalCopies { get; set; }
    }

    public class UpdateBookDto
    {
        [Required(ErrorMessage = "Tên sách không được để trống.")]
        [MaxLength(200, ErrorMessage = "Tên sách tối đa 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tác giả không được để trống.")]
        [MaxLength(100, ErrorMessage = "Tác giả tối đa 100 ký tự.")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá bìa không được để trống.")]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá bìa phải lớn hơn 1.000 VNĐ.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Thể loại không được để trống.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tổng số bản in không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Tổng số bản in phải lớn hơn 0.")]
        public int TotalCopies { get; set; }
    }

    public class BookQueryParameters
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
