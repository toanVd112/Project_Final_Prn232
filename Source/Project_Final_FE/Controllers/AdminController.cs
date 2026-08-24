using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Project_Final_FE.Models;
using Project_Final_FE.Services;

namespace Project_Final_FE.Controllers
{
    public class AdminController : Controller
    {
        private readonly IApiService _apiService;

        public AdminController(IApiService apiService)
        {
            _apiService = apiService;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        private IActionResult CheckAdminAccess()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Quản trị viên (Admin).";
                return RedirectToAction("Login", "Auth", new { returnUrl = Request.Path });
            }

            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối. Bạn không có quyền Quản trị viên.";
                return RedirectToAction("Index", "Home");
            }

            return null!;
        }

        // ==========================================
        // 1. DASHBOARD TỔNG QUAN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            // Lấy thống kê nhanh
            var booksRes = await _apiService.GetAsync<PagedResult<BookViewModel>>("books?pageSize=1");
            var categoriesRes = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
            var borrowsRes = await _apiService.GetAsync<PagedResult<BorrowRecordViewModel>>("borrows?pageSize=100");

            var totalBooks = booksRes.Data?.TotalCount ?? 0;
            var totalCategories = categoriesRes.Data?.Count ?? 0;
            var allBorrows = borrowsRes.Data?.Items ?? new List<BorrowRecordViewModel>();

            var activeBorrows = allBorrows.Count(b => b.Status == "Borrowed");
            var overdueBorrows = allBorrows.Count(b => b.IsOverdue);
            var unpaidFineBorrows = allBorrows.Count(b => !b.IsFinePaid && (b.Fine > 0 || (b.CompensationFee ?? 0) > 0));

            ViewBag.TotalBooks = totalBooks;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.ActiveBorrows = activeBorrows;
            ViewBag.OverdueBorrows = overdueBorrows;
            ViewBag.UnpaidFineBorrows = unpaidFineBorrows;
            ViewBag.RecentBorrows = allBorrows.Take(5).ToList();

            return View();
        }

        // ==========================================
        // 2. QUẢN LÝ THỂ LOẠI (CATEGORIES)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var response = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
            var categories = response.IsSuccess && response.Data != null ? response.Data : new List<CategoryViewModel>();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryFormViewModel model)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại tên thể loại.";
                return RedirectToAction("Categories");
            }

            var payload = new { Name = model.Name };
            var response = await _apiService.PostAsync<object, CategoryViewModel>("categories", payload);

            if (response.IsSuccess)
            {
                TempData["SuccessMessage"] = $"Thêm thể loại '{model.Name}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Thêm thể loại thất bại.";
            }

            return RedirectToAction("Categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(CategoryFormViewModel model)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid || !model.CategoryId.HasValue)
            {
                TempData["ErrorMessage"] = "Dữ liệu thể loại không hợp lệ.";
                return RedirectToAction("Categories");
            }

            var payload = new { Name = model.Name };
            var response = await _apiService.PutAsync($"categories/{model.CategoryId.Value}", payload);

            if (response.IsSuccess)
            {
                TempData["SuccessMessage"] = $"Cập nhật thể loại thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Cập nhật thể loại thất bại.";
            }

            return RedirectToAction("Categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var response = await _apiService.DeleteAsync($"categories/{id}");
            if (response.IsSuccess)
            {
                TempData["SuccessMessage"] = "Đã xóa thể loại thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Không thể xóa thể loại này.";
            }

            return RedirectToAction("Categories");
        }

        // ==========================================
        // 3. QUẢN LÝ KHO SÁCH (BOOKS)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Books(string? search, int? categoryId, int pageNumber = 1, int pageSize = 10)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var categoriesResponse = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
            var categories = categoriesResponse.IsSuccess && categoriesResponse.Data != null
                ? categoriesResponse.Data
                : new List<CategoryViewModel>();

            var query = $"books?search={search}&categoryId={categoryId}&pageNumber={pageNumber}&pageSize={pageSize}";
            var booksResponse = await _apiService.GetAsync<PagedResult<BookViewModel>>(query);

            var books = booksResponse.IsSuccess && booksResponse.Data != null
                ? booksResponse.Data
                : new PagedResult<BookViewModel> { PageNumber = pageNumber, PageSize = pageSize };

            var viewModel = new BookSearchViewModel
            {
                Search = search,
                CategoryId = categoryId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Books = books,
                Categories = categories
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateBook()
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var categoriesResponse = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
            var categories = categoriesResponse.IsSuccess && categoriesResponse.Data != null
                ? categoriesResponse.Data
                : new List<CategoryViewModel>();

            var model = new BookFormViewModel
            {
                Categories = categories.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
            };

            return View("BookForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBook(BookFormViewModel model)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                var categoriesRes = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
                model.Categories = categoriesRes.Data?.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name });
                return View("BookForm", model);
            }

            var payload = new
            {
                Title = model.Title,
                Author = model.Author,
                Price = model.Price,
                CategoryId = model.CategoryId,
                TotalCopies = model.TotalCopies
            };

            var response = await _apiService.PostAsync<object, BookViewModel>("books", payload);
            if (response.IsSuccess)
            {
                TempData["SuccessMessage"] = $"Thêm sách '{model.Title}' thành công!";
                return RedirectToAction("Books");
            }

            TempData["ErrorMessage"] = response.ErrorMessage ?? "Thêm sách thất bại.";
            var catRes = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
            model.Categories = catRes.Data?.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name });
            return View("BookForm", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditBook(int id)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var bookRes = await _apiService.GetAsync<BookViewModel>($"books/{id}");
            if (!bookRes.IsSuccess || bookRes.Data == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sách cần sửa.";
                return RedirectToAction("Books");
            }

            var categoriesRes = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
            var categories = categoriesRes.IsSuccess && categoriesRes.Data != null ? categoriesRes.Data : new List<CategoryViewModel>();

            var model = new BookFormViewModel
            {
                BookId = bookRes.Data.BookId,
                Title = bookRes.Data.Title,
                Author = bookRes.Data.Author,
                Price = bookRes.Data.Price,
                CategoryId = bookRes.Data.CategoryId,
                TotalCopies = bookRes.Data.TotalCopies,
                Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name,
                    Selected = c.CategoryId == bookRes.Data.CategoryId
                })
            };

            return View("BookForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(int id, BookFormViewModel model)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                var categoriesRes = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
                model.Categories = categoriesRes.Data?.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name });
                return View("BookForm", model);
            }

            var payload = new
            {
                Title = model.Title,
                Author = model.Author,
                Price = model.Price,
                CategoryId = model.CategoryId,
                TotalCopies = model.TotalCopies
            };

            var response = await _apiService.PutAsync($"books/{id}", payload);
            if (response.IsSuccess)
            {
                TempData["SuccessMessage"] = $"Cập nhật thông tin sách '{model.Title}' thành công!";
                return RedirectToAction("Books");
            }

            TempData["ErrorMessage"] = response.ErrorMessage ?? "Cập nhật sách thất bại.";
            var catRes = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
            model.Categories = catRes.Data?.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name });
            return View("BookForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var response = await _apiService.DeleteAsync($"books/{id}");
            if (response.IsSuccess)
            {
                TempData["SuccessMessage"] = "Đã xóa sách khỏi thư viện thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Không thể xóa cuốn sách này.";
            }

            return RedirectToAction("Books");
        }

        // ==========================================
        // 4. QUẢN LÝ MƯỢN / TRẢ TẠI QUẦY (BORROWS)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Borrows(string? status, bool? isFinePaid, int pageNumber = 1, int pageSize = 10)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var query = $"borrows?status={status}&isFinePaid={isFinePaid}&pageNumber={pageNumber}&pageSize={pageSize}";
            var response = await _apiService.GetAsync<PagedResult<BorrowRecordViewModel>>(query);

            var borrows = response.IsSuccess && response.Data != null
                ? response.Data
                : new PagedResult<BorrowRecordViewModel> { PageNumber = pageNumber, PageSize = pageSize };

            var model = new BorrowAdminFilterViewModel
            {
                Status = status,
                IsFinePaid = isFinePaid,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Borrows = borrows
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var response = await _apiService.PutAsync<BorrowRecordViewModel>($"borrows/{id}/return");
            if (response.IsSuccess && response.Data != null)
            {
                if (response.Data.Fine > 0)
                {
                    TempData["SuccessMessage"] = $"Xác nhận trả sách thành công! Sách bị trễ hạn, tiền phạt cần thu: {response.Data.Fine:N0} VNĐ.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Xác nhận trả sách đúng hạn thành công! Sách đã được cộng lại vào kho.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Xác nhận trả sách thất bại.";
            }

            return RedirectToAction("Borrows");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportLost(int id)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var response = await _apiService.PutAsync<BorrowRecordViewModel>($"borrows/{id}/report-lost");
            if (response.IsSuccess && response.Data != null)
            {
                var total = response.Data.Fine + (response.Data.CompensationFee ?? 0);
                TempData["SuccessMessage"] = $"Ghi nhận báo mất sách thành công! Tổng số tiền độc giả cần bồi thường (Giá bìa + Phạt trễ hạn): {total:N0} VNĐ.";
            }
            else
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Ghi nhận báo mất sách thất bại.";
            }

            return RedirectToAction("Borrows");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFine(int id)
        {
            var authCheck = CheckAdminAccess();
            if (authCheck != null) return authCheck;

            var response = await _apiService.PutAsync<BorrowRecordViewModel>($"borrows/{id}/pay-fine");
            if (response.IsSuccess && response.Data != null)
            {
                TempData["SuccessMessage"] = "Xác nhận đã thu đủ tiền phạt / bồi thường tại quầy! Độc giả đã hoàn thành nghĩa vụ tài chính.";
            }
            else
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Xác nhận thu tiền thất bại.";
            }

            return RedirectToAction("Borrows");
        }
    }
}
