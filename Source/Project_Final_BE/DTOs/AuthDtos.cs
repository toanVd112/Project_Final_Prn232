using System;
using System.ComponentModel.DataAnnotations;

namespace Project_Final_BE.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có tối thiểu 6 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
