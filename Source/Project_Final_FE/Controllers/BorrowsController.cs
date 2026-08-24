using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project_Final_FE.Models;
using Project_Final_FE.Services;

namespace Project_Final_FE.Controllers
{
    public class BorrowsController : Controller
    {
        private readonly IApiService _apiService;

        public BorrowsController(IApiService apiService)
        {
            _apiService = apiService;
        }

        private bool IsMember()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Member";
        }

        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken"));
        }

        /// <summary>
        /// UC-06: Độc giả thực hiện mượn sách trực tuyến
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrow(int bookId)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập tài khoản Độc giả để thực hiện mượn sách.";
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Details", "Home", new { id = bookId }) });
            }

            if (!IsMember())
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Độc giả (Member) mới có quyền mượn sách trực tuyến.";
                return RedirectToAction("Details", "Home", new { id = bookId });
            }

            var payload = new { BookId = bookId };
            var response = await _apiService.PostAsync<object, BorrowRecordViewModel>("borrows", payload);

            if (response.IsSuccess && response.Data != null)
            {
                TempData["SuccessMessage"] = $"Mượn sách '{response.Data.BookTitle}' thành công! Hạn trả sách là ngày {response.Data.DueDate:dd/MM/yyyy}.";
                return RedirectToAction("MyBorrows");
            }

            TempData["ErrorMessage"] = response.ErrorMessage ?? "Không thể thực hiện mượn sách. Vui lòng kiểm tra lại.";
            return RedirectToAction("Details", "Home", new { id = bookId });
        }

        /// <summary>
        /// UC-08: Xem lịch sử mượn và các khoản phạt tạm tính của Độc giả
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MyBorrows()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem lịch sử mượn sách.";
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("MyBorrows", "Borrows") });
            }

            if (!IsMember())
            {
                TempData["ErrorMessage"] = "Trang này chỉ dành cho tài khoản Độc giả (Member).";
                return RedirectToAction("Index", "Home");
            }

            var response = await _apiService.GetAsync<List<BorrowRecordViewModel>>("borrows/my");
            var borrows = response.IsSuccess && response.Data != null
                ? response.Data
                : new List<BorrowRecordViewModel>();

            var model = new MyBorrowsViewModel
            {
                Borrows = borrows
            };

            return View(model);
        }

        /// <summary>
        /// Member gửi yêu cầu trả sách. Việc gửi yêu cầu không hoàn tất lượt mượn;
        /// Admin chỉ xác nhận sau khi nhận sách vật lý tại quầy.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(int id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để gửi yêu cầu trả sách.";
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("MyBorrows", "Borrows") });
            }

            if (!IsMember())
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Độc giả (Member) mới có thể gửi yêu cầu trả sách.";
                return RedirectToAction("Index", "Home");
            }

            var response = await _apiService.PutAsync<BorrowRecordViewModel>($"borrows/{id}/request-return");
            if (response.IsSuccess && response.Data != null)
            {
                TempData["SuccessMessage"] = response.Data.EstimatedFine > 0
                    ? $"Đã gửi yêu cầu trả sách '{response.Data.BookTitle}'. Phí trễ hạn tạm tính hiện tại: {response.Data.EstimatedFine:N0} VNĐ. Vui lòng mang sách đến quầy để Thủ thư xác nhận."
                    : $"Đã gửi yêu cầu trả sách '{response.Data.BookTitle}'. Vui lòng mang sách đến quầy để Thủ thư kiểm tra và xác nhận.";
            }
            else
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Không thể gửi yêu cầu trả sách. Vui lòng thử lại.";
            }

            return RedirectToAction("MyBorrows");
        }
    }
}
