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
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// UC-04: Lấy danh sách thể loại sách (kèm số lượng sách)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    BookCount = c.Books.Count
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Lấy chi tiết thể loại theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    BookCount = c.Books.Count
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(new { message = $"Không tìm thấy thể loại có ID = {id}." });
            }

            return Ok(category);
        }

        /// <summary>
        /// UC-04: Thêm thể loại mới (Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var normalizedName = dto.Name.Trim();
            var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == normalizedName.ToLower());
            if (exists)
            {
                return BadRequest(new { message = $"Thể loại '{normalizedName}' đã tồn tại." });
            }

            var category = new Category
            {
                Name = normalizedName
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var result = new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                BookCount = 0
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, result);
        }

        /// <summary>
        /// UC-04: Cập nhật thể loại (Admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Không tìm thấy thể loại có ID = {id}." });
            }

            var normalizedName = dto.Name.Trim();
            var duplicate = await _context.Categories.AnyAsync(c => c.CategoryId != id && c.Name.ToLower() == normalizedName.ToLower());
            if (duplicate)
            {
                return BadRequest(new { message = $"Thể loại '{normalizedName}' đã tồn tại." });
            }

            category.Name = normalizedName;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// UC-04: Xóa thể loại (Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { message = $"Không tìm thấy thể loại có ID = {id}." });
            }

            // BR-09: Không được xóa một thể loại nếu vẫn còn ít nhất một cuốn sách đang tham chiếu
            if (category.Books.Any())
            {
                return BadRequest(new { message = "Không thể xóa thể loại này vì vẫn còn sách thuộc thể loại trong hệ thống." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
