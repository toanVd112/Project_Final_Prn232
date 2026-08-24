using System.Threading.Tasks;
using Project_Final_FE.Models;

namespace Project_Final_FE.Services
{
    public interface IApiService
    {
        Task<ApiResponse<T>> GetAsync<T>(string endpoint);
        Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
        Task<ApiResponse<bool>> PutAsync<TRequest>(string endpoint, TRequest data);
        Task<ApiResponse<TResponse>> PutAsync<TResponse>(string endpoint);
        Task<ApiResponse<bool>> DeleteAsync(string endpoint);
    }
}
