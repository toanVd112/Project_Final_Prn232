using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Project_Final_BE.DTOs;
using Project_Final_BE.Models;

namespace Project_Final_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        /// <summary>
        /// UC-01: Đăng ký tài khoản Member mới
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // BR-01: Kiểm tra tính duy nhất của Email
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email này đã được sử dụng trong hệ thống." });
            }

            // BR-03: Mặc định tài khoản đăng ký có vai trò Member
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                EmailConfirmed = true
            };

            // BR-02: Password rule kiểm tra bởi Identity
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Đăng ký không thành công.", errors = result.Errors });
            }

            await _userManager.AddToRoleAsync(user, "Member");

            return StatusCode(201, new { message = "Đăng ký tài khoản thành công. Bạn có thể đăng nhập ngay." });
        }

        /// <summary>
        /// UC-02: Đăng nhập hệ thống và nhận JWT Token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.Count > 0 ? roles[0] : "Member";

            // BR-04: JWT Token với Expiration
            var jwtKey = _configuration["Jwt:Key"] ?? "DefaultSecretKeyForLibraryManagement2026!";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "LibraryManagementBE";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "LibraryManagementClient";
            var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryInMinutes"], out var mins) ? mins : 60;

            var expiration = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, primaryRole)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new AuthResponseDto
            {
                Token = tokenString,
                Expiration = expiration,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = primaryRole
            });
        }
    }
}
