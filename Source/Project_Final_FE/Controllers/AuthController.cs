using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project_Final_FE.Models;
using Project_Final_FE.Services;

namespace Project_Final_FE.Controllers
{
    public class AuthController : Controller
    {
        private readonly IApiService _apiService;

        public AuthController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.PostAsync<LoginViewModel, AuthResponseViewModel>("auth/login", model);
            if (response.IsSuccess && response.Data != null)
            {
                // Lưu token và thông tin người dùng vào Session
                HttpContext.Session.SetString("JwtToken", response.Data.Token);
                HttpContext.Session.SetString("UserEmail", response.Data.Email);
                HttpContext.Session.SetString("UserFullName", response.Data.FullName);
                HttpContext.Session.SetString("UserRole", response.Data.Role);

                TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {response.Data.FullName}.";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (response.Data.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Đăng nhập thất bại. Vui lòng kiểm tra lại email hoặc mật khẩu.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var registerPayload = new
            {
                Email = model.Email,
                Password = model.Password,
                FullName = model.FullName
            };

            var response = await _apiService.PostAsync<object, object>("auth/register", registerPayload);
            if (response.IsSuccess)
            {
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Đăng ký không thành công. Vui lòng thử lại.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }
    }
}
