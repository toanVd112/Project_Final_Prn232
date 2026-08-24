using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Project_Final_BE.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
