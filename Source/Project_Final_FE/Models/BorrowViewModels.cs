using System;
using System.Collections.Generic;

namespace Project_Final_FE.Models
{
    public class BorrowRecordViewModel
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
        public DateTime? ReturnRequestedAt { get; set; }
        public string Status { get; set; } = string.Empty; // Borrowed, Returned, Lost
        public decimal Fine { get; set; }
        public decimal EstimatedFine { get; set; }
        public decimal? CompensationFee { get; set; }
        public bool IsFinePaid { get; set; }
        public DateTime? FinePaidDate { get; set; }

        public bool IsOverdue => Status == "Borrowed" && DateTime.UtcNow.Date > DueDate.Date;
        public bool IsReturnRequested => Status == "Borrowed" && ReturnRequestedAt.HasValue;
        public int DaysOverdue => IsOverdue ? (int)Math.Ceiling((DateTime.UtcNow.Date - DueDate.Date).TotalDays) : 0;
    }

    public class MyBorrowsViewModel
    {
        public List<BorrowRecordViewModel> Borrows { get; set; } = new List<BorrowRecordViewModel>();
        public int ActiveBorrowCount => Borrows.FindAll(b => b.Status == "Borrowed").Count;
        public int OverdueCount => Borrows.FindAll(b => b.IsOverdue).Count;
        public decimal TotalUnpaidFine => Borrows.FindAll(b => !b.IsFinePaid).ConvertAll(b => (b.Status == "Borrowed" ? b.EstimatedFine : b.Fine) + (b.CompensationFee ?? 0)).Sum();
    }

    public class BorrowAdminFilterViewModel
    {
        public string? UserId { get; set; }
        public string? Status { get; set; }
        public bool? IsFinePaid { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public PagedResult<BorrowRecordViewModel> Borrows { get; set; } = new PagedResult<BorrowRecordViewModel>();
    }
}
