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
    public class BorrowsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BorrowsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private static decimal CalculateEstimatedFine(BorrowRecord record)
        {
            if (record.Status == "Borrowed" && DateTime.UtcNow > record.DueDate)
            {
                var daysLate = (int)Math.Ceiling((DateTime.UtcNow.Date - record.DueDate.Date).TotalDays);
                return daysLate > 0 ? daysLate * 5000m : 0;
            }
            return record.Fine;
        }

        /// <summary>
        /// UC-06: Mượn sách trực tuyến (Member)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Member")]
        public async Task<ActionResult<BorrowRecordDto>> BorrowBook([FromBody] BorrowRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Không xác định được danh tính người dùng." });
            }

            // BR-14: Kiểm tra Member có sách quá hạn chưa trả hay không
            var hasOverdueBook = await _context.BorrowRecords
                .AnyAsync(br => br.UserId == userId && br.Status == "Borrowed" && DateTime.UtcNow > br.DueDate);
            if (hasOverdueBook)
            {
                return BadRequest(new { message = "Bạn đang có sách trễ hạn chưa trả. Vui lòng mang sách đến quầy hoàn trả trước khi mượn cuốn mới." });
            }

            // BR-15: Kiểm tra Member có khoản phạt / bồi thường chưa nộp hay không
            var hasUnpaidFine = await _context.BorrowRecords
                .AnyAsync(br => br.UserId == userId && (br.Fine > 0 || (br.CompensationFee.HasValue && br.CompensationFee.Value > 0)) && !br.IsFinePaid);
            if (hasUnpaidFine)
            {
                return BadRequest(new { message = "Bạn đang có khoản nợ tiền phạt hoặc bồi thường chưa thanh toán. Vui lòng nộp phạt tại quầy thư viện." });
            }

            // BR-13: Kiểm tra giới hạn mượn tối đa 5 cuốn
            var activeBorrowCount = await _context.BorrowRecords
                .CountAsync(br => br.UserId == userId && br.Status == "Borrowed");
            if (activeBorrowCount >= 5)
            {
                return BadRequest(new { message = "Bạn đã đạt giới hạn mượn tối đa 5 cuốn sách cùng lúc. Vui lòng trả bớt sách trước khi mượn tiếp." });
            }

            // BR-24: Sử dụng Database Transaction & Concurrency Token để chống Race Condition
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var book = await _context.Books.FindAsync(dto.BookId);
                if (book == null)
                {
                    return NotFound(new { message = $"Không tìm thấy sách có ID = {dto.BookId}." });
                }

                // BR-11: Kiểm tra AvailableCopies > 0
                if (book.AvailableCopies <= 0)
                {
                    return BadRequest(new { message = "Sách hiện tại đã hết bản sẵn sàng cho mượn." });
                }

                // Giảm AvailableCopies
                book.AvailableCopies -= 1;

                var now = DateTime.UtcNow;
                var borrowRecord = new BorrowRecord
                {
                    UserId = userId,
                    BookId = book.BookId,
                    BorrowDate = now,
                    DueDate = now.AddDays(14), // BR-12: Hạn mượn 14 ngày
                    Status = "Borrowed",
                    Fine = 0,
                    IsFinePaid = false
                };

                _context.BorrowRecords.Add(borrowRecord);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var user = await _context.Users.FindAsync(userId);

                var resultDto = new BorrowRecordDto
                {
                    BorrowRecordId = borrowRecord.BorrowRecordId,
                    UserId = userId,
                    UserFullName = user?.FullName ?? string.Empty,
                    UserEmail = user?.Email ?? string.Empty,
                    BookId = book.BookId,
                    BookTitle = book.Title,
                    BookAuthor = book.Author,
                    BorrowDate = borrowRecord.BorrowDate,
                    DueDate = borrowRecord.DueDate,
                    ReturnDate = borrowRecord.ReturnDate,
                    Status = borrowRecord.Status,
                    Fine = 0,
                    EstimatedFine = 0,
                    CompensationFee = null,
                    IsFinePaid = false,
                    FinePaidDate = null
                };

                return CreatedAtAction(nameof(GetBorrowById), new { id = borrowRecord.BorrowRecordId }, resultDto);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = "Đã xảy ra tranh chấp dữ liệu khi mượn sách. Vui lòng thử lại sau giây lát." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý mượn sách.", detail = ex.Message });
            }
        }

        /// <summary>
        /// UC-08: Xem lịch sử mượn & Phạt tạm tính của bản thân (Member)
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Member")]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetMyBorrows()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Không xác định được danh tính người dùng." });
            }

            var records = await _context.BorrowRecords
                .Include(br => br.Book)
                .Include(br => br.User)
                .Where(br => br.UserId == userId)
                .OrderByDescending(br => br.BorrowDate)
                .ToListAsync();

            var dtos = records.Select(br => new BorrowRecordDto
            {
                BorrowRecordId = br.BorrowRecordId,
                UserId = br.UserId,
                UserFullName = br.User.FullName,
                UserEmail = br.User.Email ?? string.Empty,
                BookId = br.BookId,
                BookTitle = br.Book.Title,
                BookAuthor = br.Book.Author,
                BorrowDate = br.BorrowDate,
                DueDate = br.DueDate,
                ReturnDate = br.ReturnDate,
                Status = br.Status,
                Fine = br.Fine,
                EstimatedFine = CalculateEstimatedFine(br), // BR-27: Phạt tạm tính thời gian thực
                CompensationFee = br.CompensationFee,
                IsFinePaid = br.IsFinePaid,
                FinePaidDate = br.FinePaidDate
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// UC-09: Quản lý toàn bộ danh sách mượn/trả có phân trang & lọc (Admin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResult<BorrowRecordDto>>> GetBorrows([FromQuery] BorrowQueryParameters query)
        {
            var borrowsQuery = _context.BorrowRecords
                .Include(br => br.Book)
                .Include(br => br.User)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                borrowsQuery = borrowsQuery.Where(br => br.UserId == query.UserId);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                borrowsQuery = borrowsQuery.Where(br => br.Status.ToLower() == query.Status.Trim().ToLower());
            }

            if (query.IsFinePaid.HasValue)
            {
                borrowsQuery = borrowsQuery.Where(br => br.IsFinePaid == query.IsFinePaid.Value);
            }

            var totalCount = await borrowsQuery.CountAsync();
            var pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
            var pageSize = query.PageSize > 0 ? query.PageSize : 10;

            var records = await borrowsQuery
                .OrderByDescending(br => br.BorrowDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = records.Select(br => new BorrowRecordDto
            {
                BorrowRecordId = br.BorrowRecordId,
                UserId = br.UserId,
                UserFullName = br.User.FullName,
                UserEmail = br.User.Email ?? string.Empty,
                BookId = br.BookId,
                BookTitle = br.Book.Title,
                BookAuthor = br.Book.Author,
                BorrowDate = br.BorrowDate,
                DueDate = br.DueDate,
                ReturnDate = br.ReturnDate,
                Status = br.Status,
                Fine = br.Fine,
                EstimatedFine = CalculateEstimatedFine(br),
                CompensationFee = br.CompensationFee,
                IsFinePaid = br.IsFinePaid,
                FinePaidDate = br.FinePaidDate
            }).ToList();

            return Ok(new PagedResult<BorrowRecordDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Xem chi tiết một bản ghi mượn (Admin hoặc Chủ sở hữu)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BorrowRecordDto>> GetBorrowById(int id)
        {
            var record = await _context.BorrowRecords
                .Include(br => br.Book)
                .Include(br => br.User)
                .FirstOrDefaultAsync(br => br.BorrowRecordId == id);

            if (record == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi mượn có ID = {id}." });
            }

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // BR-22: Member chỉ được xem lượt mượn của chính mình
            if (!isAdmin && record.UserId != userId)
            {
                return Forbid();
            }

            var dto = new BorrowRecordDto
            {
                BorrowRecordId = record.BorrowRecordId,
                UserId = record.UserId,
                UserFullName = record.User.FullName,
                UserEmail = record.User.Email ?? string.Empty,
                BookId = record.BookId,
                BookTitle = record.Book.Title,
                BookAuthor = record.Book.Author,
                BorrowDate = record.BorrowDate,
                DueDate = record.DueDate,
                ReturnDate = record.ReturnDate,
                Status = record.Status,
                Fine = record.Fine,
                EstimatedFine = CalculateEstimatedFine(record),
                CompensationFee = record.CompensationFee,
                IsFinePaid = record.IsFinePaid,
                FinePaidDate = record.FinePaidDate
            };

            return Ok(dto);
        }

        /// <summary>
        /// UC-07: Xác nhận nhận lại sách tại quầy (Admin)
        /// </summary>
        [HttpPut("{id}/return")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BorrowRecordDto>> ReturnBook(int id)
        {
            var record = await _context.BorrowRecords
                .Include(br => br.Book)
                .Include(br => br.User)
                .FirstOrDefaultAsync(br => br.BorrowRecordId == id);

            if (record == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi mượn có ID = {id}." });
            }

            if (record.Status != "Borrowed")
            {
                return BadRequest(new { message = $"Bản ghi mượn này đã ở trạng thái '{record.Status}', không thể thực hiện trả sách." });
            }

            var returnDate = DateTime.UtcNow;
            record.ReturnDate = returnDate;
            record.Status = "Returned";

            // BR-17 & BR-18: Tính phí phạt trễ hạn nếu trả sau DueDate
            if (returnDate.Date > record.DueDate.Date)
            {
                var daysLate = (int)Math.Ceiling((returnDate.Date - record.DueDate.Date).TotalDays);
                record.Fine = daysLate * 5000m;
                record.IsFinePaid = false; // Phải nộp phạt
            }
            else
            {
                record.Fine = 0;
                record.IsFinePaid = true; // Đúng hạn không có phạt
            }

            // BR-20: Tăng lại AvailableCopies lên 1
            record.Book.AvailableCopies += 1;

            await _context.SaveChangesAsync();

            var dto = new BorrowRecordDto
            {
                BorrowRecordId = record.BorrowRecordId,
                UserId = record.UserId,
                UserFullName = record.User.FullName,
                UserEmail = record.User.Email ?? string.Empty,
                BookId = record.BookId,
                BookTitle = record.Book.Title,
                BookAuthor = record.Book.Author,
                BorrowDate = record.BorrowDate,
                DueDate = record.DueDate,
                ReturnDate = record.ReturnDate,
                Status = record.Status,
                Fine = record.Fine,
                EstimatedFine = 0,
                CompensationFee = record.CompensationFee,
                IsFinePaid = record.IsFinePaid,
                FinePaidDate = record.FinePaidDate
            };

            return Ok(dto);
        }

        /// <summary>
        /// UC-10: Xử lý báo mất sách và tính phí bồi thường (Admin)
        /// </summary>
        [HttpPut("{id}/report-lost")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BorrowRecordDto>> ReportLost(int id)
        {
            var record = await _context.BorrowRecords
                .Include(br => br.Book)
                .Include(br => br.User)
                .FirstOrDefaultAsync(br => br.BorrowRecordId == id);

            if (record == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi mượn có ID = {id}." });
            }

            if (record.Status != "Borrowed")
            {
                return BadRequest(new { message = $"Bản ghi mượn này đã ở trạng thái '{record.Status}', không thể báo mất sách." });
            }

            var now = DateTime.UtcNow;
            record.Status = "Lost";

            // Tính tiền phạt trễ hạn tính đến thời điểm báo mất (nếu quá hạn)
            if (now.Date > record.DueDate.Date)
            {
                var daysLate = (int)Math.Ceiling((now.Date - record.DueDate.Date).TotalDays);
                record.Fine = daysLate * 5000m;
            }
            else
            {
                record.Fine = 0;
            }

            // BR-26: Phí bồi thường = Giá bìa sách (Book.Price)
            record.CompensationFee = record.Book.Price;
            record.IsFinePaid = false;

            // BR-26: Giảm vĩnh viễn TotalCopies đi 1 (sách bị loại bỏ khỏi thư viện, không tăng AvailableCopies)
            record.Book.TotalCopies -= 1;

            await _context.SaveChangesAsync();

            var dto = new BorrowRecordDto
            {
                BorrowRecordId = record.BorrowRecordId,
                UserId = record.UserId,
                UserFullName = record.User.FullName,
                UserEmail = record.User.Email ?? string.Empty,
                BookId = record.BookId,
                BookTitle = record.Book.Title,
                BookAuthor = record.Book.Author,
                BorrowDate = record.BorrowDate,
                DueDate = record.DueDate,
                ReturnDate = record.ReturnDate,
                Status = record.Status,
                Fine = record.Fine,
                EstimatedFine = 0,
                CompensationFee = record.CompensationFee,
                IsFinePaid = record.IsFinePaid,
                FinePaidDate = record.FinePaidDate
            };

            return Ok(dto);
        }

        /// <summary>
        /// UC-11: Xác nhận thu tiền phạt / bồi thường tại quầy (Admin)
        /// </summary>
        [HttpPut("{id}/pay-fine")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BorrowRecordDto>> PayFine(int id)
        {
            var record = await _context.BorrowRecords
                .Include(br => br.Book)
                .Include(br => br.User)
                .FirstOrDefaultAsync(br => br.BorrowRecordId == id);

            if (record == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi mượn có ID = {id}." });
            }

            var totalFee = record.Fine + (record.CompensationFee ?? 0);
            if (totalFee <= 0 || record.IsFinePaid)
            {
                return BadRequest(new { message = "Bản ghi này không có khoản phạt/bồi thường cần thanh toán hoặc đã được nộp đủ trước đó." });
            }

            // BR-21: Xác nhận thu tiền
            record.IsFinePaid = true;
            record.FinePaidDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new BorrowRecordDto
            {
                BorrowRecordId = record.BorrowRecordId,
                UserId = record.UserId,
                UserFullName = record.User.FullName,
                UserEmail = record.User.Email ?? string.Empty,
                BookId = record.BookId,
                BookTitle = record.Book.Title,
                BookAuthor = record.Book.Author,
                BorrowDate = record.BorrowDate,
                DueDate = record.DueDate,
                ReturnDate = record.ReturnDate,
                Status = record.Status,
                Fine = record.Fine,
                EstimatedFine = 0,
                CompensationFee = record.CompensationFee,
                IsFinePaid = record.IsFinePaid,
                FinePaidDate = record.FinePaidDate
            };

            return Ok(dto);
        }
    }
}
