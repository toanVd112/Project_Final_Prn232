using System;
using System.ComponentModel.DataAnnotations;

namespace Project_Final_BE.DTOs
{
    public class BorrowRequestDto
    {
        [Required(ErrorMessage = "Mã sách không được để trống.")]
        public int BookId { get; set; }
    }

    public class BorrowRecordDto
    {
        public int BorrowRecordId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty; // Borrowed, Returned, Lost
        public decimal Fine { get; set; }
        public decimal EstimatedFine { get; set; }
        public decimal? CompensationFee { get; set; }
        public bool IsFinePaid { get; set; }
        public DateTime? FinePaidDate { get; set; }
    }

    public class BorrowQueryParameters
    {
        public string? UserId { get; set; }
        public string? Status { get; set; } // Borrowed, Returned, Lost
        public bool? IsFinePaid { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
