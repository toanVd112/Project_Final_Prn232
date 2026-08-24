using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Project_Final_FE.Models
{
    public class BookViewModel
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

    public class BookFormViewModel
    {
        public int? BookId { get; set; }

        [Required(ErrorMessage = "Tên sách không được để trống.")]
        [MaxLength(200, ErrorMessage = "Tên sách tối đa 200 ký tự.")]
        [Display(Name = "Tên đầu sách")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tác giả không được để trống.")]
        [MaxLength(100, ErrorMessage = "Tác giả tối đa 100 ký tự.")]
        [Display(Name = "Tác giả")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá bìa không được để trống.")]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá bìa phải lớn hơn 1.000 VNĐ.")]
        [Display(Name = "Giá bìa (VNĐ)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thể loại.")]
        [Display(Name = "Thể loại")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tổng số bản in không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Tổng số bản in phải lớn hơn 0.")]
        [Display(Name = "Tổng số bản in")]
        public int TotalCopies { get; set; }

        public IEnumerable<SelectListItem>? Categories { get; set; }
    }

    public class BookSearchViewModel
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public PagedResult<BookViewModel> Books { get; set; } = new PagedResult<BookViewModel>();
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
    }
}
