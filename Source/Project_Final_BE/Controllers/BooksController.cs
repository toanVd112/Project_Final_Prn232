using System.Collections.Generic;
using System.Linq;
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
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// UC-05: Tìm kiếm, lọc và xem danh mục sách có phân trang (Public)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<BookDto>>> GetBooks([FromQuery] BookQueryParameters query)
        {
            var booksQuery = _context.Books
                .Include(b => b.Category)
                .AsNoTracking()
                .AsQueryable();

            // Tìm kiếm theo từ khóa (Tên sách hoặc Tác giả)
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.Trim().ToLower();
                booksQuery = booksQuery.Where(b => b.Title.ToLower().Contains(searchTerm) ||
                                                   b.Author.ToLower().Contains(searchTerm));
            }

            // Lọc theo thể loại
            if (query.CategoryId.HasValue && query.CategoryId.Value > 0)
            {
                booksQuery = booksQuery.Where(b => b.CategoryId == query.CategoryId.Value);
            }

            var totalCount = await booksQuery.CountAsync();

            var pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
            var pageSize = query.PageSize > 0 ? query.PageSize : 10;

            var items = await booksQuery
                .OrderByDescending(b => b.BookId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Price = b.Price,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category.Name,
                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies
                })
                .ToListAsync();

            return Ok(new PagedResult<BookDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Xem thông tin chi tiết một cuốn sách
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .AsNoTracking()
                .Where(b => b.BookId == id)
                .Select(b => new BookDto
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Price = b.Price,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category.Name,
                    TotalCopies = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies
                })
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound(new { message = $"Không tìm thấy sách có ID = {id}." });
            }

            return Ok(book);
        }

        /// <summary>
        /// UC-03: Thêm sách mới vào thư viện (Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BookDto>> CreateBook([FromBody] CreateBookDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // BR-06 & BR-07: Kiểm tra Category tồn tại, Price > 0, TotalCopies > 0
            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                return BadRequest(new { message = "Thể loại đã chọn không tồn tại." });
            }

            var book = new Book
            {
                Title = dto.Title.Trim(),
                Author = dto.Author.Trim(),
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                TotalCopies = dto.TotalCopies,
                AvailableCopies = dto.TotalCopies // Ban đầu toàn bộ sách sẵn sàng mượn
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var result = new BookDto
            {
                BookId = book.BookId,
                Title = book.Title,
                Author = book.Author,
                Price = book.Price,
                CategoryId = book.CategoryId,
                CategoryName = category.Name,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies
            };

            return CreatedAtAction(nameof(GetBook), new { id = book.BookId }, result);
        }

        /// <summary>
        /// UC-03: Cập nhật thông tin sách (Admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound(new { message = $"Không tìm thấy sách có ID = {id}." });
            }

            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                return BadRequest(new { message = "Thể loại đã chọn không tồn tại." });
            }

            // Tính toán chênh lệch số lượng TotalCopies để điều chỉnh AvailableCopies tương ứng
            var diff = dto.TotalCopies - book.TotalCopies;
            var newAvailable = book.AvailableCopies + diff;
            if (newAvailable < 0)
            {
                return BadRequest(new { message = $"Không thể giảm tổng số bản in xuống {dto.TotalCopies} vì hiện đang có {book.TotalCopies - book.AvailableCopies} cuốn đang được mượn." });
            }

            book.Title = dto.Title.Trim();
            book.Author = dto.Author.Trim();
            book.Price = dto.Price;
            book.CategoryId = dto.CategoryId;
            book.TotalCopies = dto.TotalCopies;
            book.AvailableCopies = newAvailable;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// UC-03: Xóa sách khỏi thư viện (Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound(new { message = $"Không tìm thấy sách có ID = {id}." });
            }

            // BR-10: Không được xóa một cuốn sách nếu đang tồn tại bản ghi mượn ở trạng thái "Borrowed"
            var hasActiveBorrows = await _context.BorrowRecords
                .AnyAsync(br => br.BookId == id && br.Status == "Borrowed");

            if (hasActiveBorrows)
            {
                return BadRequest(new { message = "Không thể xóa cuốn sách này vì đang có độc giả mượn chưa hoàn trả." });
            }

            // Đồng thời kiểm tra nếu có bản ghi lịch sử thì không thể xóa cứng do ràng buộc Restrict
            var hasAnyRecords = await _context.BorrowRecords.AnyAsync(br => br.BookId == id);
            if (hasAnyRecords)
            {
                return BadRequest(new { message = "Không thể xóa cuốn sách này vì đã tồn tại trong lịch sử mượn/trả của thư viện." });
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
