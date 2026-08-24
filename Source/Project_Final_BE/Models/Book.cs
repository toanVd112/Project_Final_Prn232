using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Final_BE.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Author { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá bìa phải lớn hơn 0.")]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public virtual Category Category { get; set; } = null!;

        [Range(1, int.MaxValue, ErrorMessage = "Tổng số bản in phải lớn hơn 0.")]
        public int TotalCopies { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số bản khả dụng không được âm.")]
        public int AvailableCopies { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation property
        public virtual ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    }
}
