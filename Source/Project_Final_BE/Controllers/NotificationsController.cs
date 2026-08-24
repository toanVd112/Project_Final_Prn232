using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Final_BE.Data;
using Project_Final_BE.DTOs;
using Project_Final_BE.Models;

namespace Project_Final_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private static string FormatTimeAgo(DateTime createdAt)
        {
            var span = DateTime.UtcNow - createdAt;
            if (span.TotalMinutes < 1)
                return "Vừa xong";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays} ngày trước";
            return createdAt.ToString("dd/MM/yyyy HH:mm");
        }

        /// <summary>
        /// Lấy danh sách thông báo và tự động quét nhắc nhở sắp đến hạn trả sách (trước 2 ngày)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<NotificationSummaryDto>> GetMyNotifications()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Không xác định được danh tính người dùng." });
            }

            var now = DateTime.UtcNow;

            // 1. Quét tự động các sách sắp đến hạn trả (còn <= 2 ngày) của Member để sinh thông báo nhắc nhở
            var nearDueRecords = await _context.BorrowRecords
                .Include(br => br.Book)
                .Where(br => br.UserId == userId 
                          && br.Status == "Borrowed" 
                          && br.DueDate >= now.Date
                          && br.DueDate <= now.Date.AddDays(2).AddHours(23).AddMinutes(59))
                .ToListAsync();

            if (nearDueRecords.Any())
            {
                var existingReminderIds = await _context.Notifications
                    .Where(n => n.UserId == userId && n.Type == "DueDateReminder" && n.RelatedId.HasValue)
                    .Select(n => n.RelatedId!.Value)
                    .ToListAsync();

                bool hasNewReminder = false;
                foreach (var record in nearDueRecords)
                {
                    if (!existingReminderIds.Contains(record.BorrowRecordId))
                    {
                        var daysLeft = (int)Math.Ceiling((record.DueDate.Date - now.Date).TotalDays);
                        string reminderMessage = daysLeft == 0
                            ? $"Hôm nay ({record.DueDate:dd/MM/yyyy}) là hạn chót hoàn trả cuốn sách '{record.Book.Title}'. Vui lòng mang sách đến quầy để tránh phát sinh phí trễ hạn."
                            : $"Cuốn sách '{record.Book.Title}' bạn đang mượn sẽ đến hạn trả vào ngày {record.DueDate:dd/MM/yyyy} (còn lại {daysLeft} ngày). Vui lòng chuẩn bị hoàn trả sách.";

                        _context.Notifications.Add(new Notification
                        {
                            UserId = userId,
                            Title = "Nhắc nhở: Sắp đến hạn trả sách",
                            Message = reminderMessage,
                            Type = "DueDateReminder",
                            IsRead = false,
                            CreatedAt = now,
                            RelatedId = record.BorrowRecordId
                        });
                        hasNewReminder = true;
                    }
                }

                if (hasNewReminder)
                {
                    await _context.SaveChangesAsync();
                }
            }

            // 2. Lấy danh sách thông báo của người dùng
            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    RelatedId = n.RelatedId
                })
                .ToListAsync();

            foreach (var notif in notifications)
            {
                notif.TimeAgo = FormatTimeAgo(notif.CreatedAt);
            }

            return Ok(new NotificationSummaryDto
            {
                UnreadCount = unreadCount,
                Notifications = notifications
            });
        }

        /// <summary>
        /// Đánh dấu 1 thông báo là đã đọc
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound(new { message = "Không tìm thấy thông báo." });
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã đánh dấu thông báo là đã đọc." });
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo của người dùng là đã đọc
        /// </summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notif in unreadNotifications)
                {
                    notif.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã đánh dấu tất cả thông báo là đã đọc." });
        }
    }
}
