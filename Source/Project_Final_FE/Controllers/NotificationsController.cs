using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Project_Final_FE.Models;
using Project_Final_FE.Services;

namespace Project_Final_FE.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly IApiService _apiService;

        public NotificationsController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSummary()
        {
            var response = await _apiService.GetAsync<NotificationSummaryViewModel>("notifications");
            if (response.IsSuccess && response.Data != null)
            {
                return Json(response.Data);
            }
            return Json(new NotificationSummaryViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var response = await _apiService.PutAsync<object>($"notifications/{id}/read");
            return Json(new { success = response.IsSuccess });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var response = await _apiService.PutAsync<object>("notifications/read-all");
            return Json(new { success = response.IsSuccess });
        }
    }
}
