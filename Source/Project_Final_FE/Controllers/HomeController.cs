using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Project_Final_FE.Models;
using Project_Final_FE.Services;

namespace Project_Final_FE.Controllers
{
    public class HomeController : Controller
    {
        private readonly IApiService _apiService;

        public HomeController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? categoryId, int pageNumber = 1, int pageSize = 8)
        {
            // 1. Lấy danh sách thể loại để hiển thị bộ lọc
            var categoriesResponse = await _apiService.GetAsync<List<CategoryViewModel>>("categories");
            var categories = categoriesResponse.IsSuccess && categoriesResponse.Data != null
                ? categoriesResponse.Data
                : new List<CategoryViewModel>();

            // 2. Lấy danh sách sách có phân trang và lọc
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
        public async Task<IActionResult> Details(int id)
        {
            var response = await _apiService.GetAsync<BookViewModel>($"books/{id}");
            if (!response.IsSuccess || response.Data == null)
            {
                TempData["ErrorMessage"] = response.ErrorMessage ?? "Không tìm thấy thông tin cuốn sách này.";
                return RedirectToAction("Index");
            }

            return View(response.Data);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
