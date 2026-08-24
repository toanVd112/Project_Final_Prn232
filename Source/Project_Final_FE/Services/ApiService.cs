using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Project_Final_FE.Models;

namespace Project_Final_FE.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private void AttachBearerToken()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        private static string ExtractErrorMessage(string responseBody, int statusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("message", out var msgElem))
                {
                    return msgElem.GetString() ?? $"Lỗi HTTP {statusCode}";
                }
                if (doc.RootElement.TryGetProperty("title", out var titleElem))
                {
                    return titleElem.GetString() ?? $"Lỗi HTTP {statusCode}";
                }
            }
            catch
            {
                // Not JSON or plain text
            }
            return !string.IsNullOrWhiteSpace(responseBody) ? responseBody : $"Yêu cầu thất bại với mã lỗi HTTP {statusCode}.";
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                AttachBearerToken();
                var response = await _httpClient.GetAsync(endpoint);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    return new ApiResponse<T>
                    {
                        IsSuccess = true,
                        Data = data,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    ErrorMessage = ExtractErrorMessage(content, (int)response.StatusCode),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Không thể kết nối đến máy chủ API: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                AttachBearerToken();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var resultData = default(TResponse);
                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        resultData = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);
                    }

                    return new ApiResponse<TResponse>
                    {
                        IsSuccess = true,
                        Data = resultData,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = ExtractErrorMessage(responseBody, (int)response.StatusCode),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Không thể kết nối đến máy chủ API: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<bool>> PutAsync<TRequest>(string endpoint, TRequest data)
        {
            try
            {
                AttachBearerToken();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool>
                    {
                        IsSuccess = true,
                        Data = true,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    ErrorMessage = ExtractErrorMessage(responseBody, (int)response.StatusCode),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    ErrorMessage = $"Không thể kết nối đến máy chủ API: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<TResponse>> PutAsync<TResponse>(string endpoint)
        {
            try
            {
                AttachBearerToken();
                var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var resultData = default(TResponse);
                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        resultData = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);
                    }

                    return new ApiResponse<TResponse>
                    {
                        IsSuccess = true,
                        Data = resultData,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = ExtractErrorMessage(responseBody, (int)response.StatusCode),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Không thể kết nối đến máy chủ API: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
        {
            try
            {
                AttachBearerToken();
                var response = await _httpClient.DeleteAsync(endpoint);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool>
                    {
                        IsSuccess = true,
                        Data = true,
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    ErrorMessage = ExtractErrorMessage(responseBody, (int)response.StatusCode),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    ErrorMessage = $"Không thể kết nối đến máy chủ API: {ex.Message}",
                    StatusCode = 500
                };
            }
        }
    }
}
