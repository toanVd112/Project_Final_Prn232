using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Final_BE.Models
{
    public class BorrowRecord
    {
        [Key]
        public int BorrowRecordId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public int BookId { get; set; }

        public virtual Book Book { get; set; } = null!;

        [Required]
        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Borrowed"; // Borrowed, Returned, Lost

        [Column(TypeName = "decimal(18,2)")]
        public decimal Fine { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CompensationFee { get; set; }

        public bool IsFinePaid { get; set; } = false;

        public DateTime? FinePaidDate { get; set; }
    }
}
