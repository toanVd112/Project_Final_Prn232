using System;
using System.Collections.Generic;

namespace Project_Final_FE.Models
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // BorrowSuccess, AdminNewBorrow, DueDateReminder, General
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedId { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    public class NotificationSummaryViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationViewModel> Notifications { get; set; } = new List<NotificationViewModel>();
    }
}
