using System;
using System.Collections.Generic;

namespace Project_Final_BE.DTOs
{
    public class NotificationDto
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

    public class NotificationSummaryDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();
    }
}
