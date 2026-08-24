using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project_Final_BE.Models;

namespace Project_Final_BE.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Seed Roles
            string[] roles = { "Admin", "Member" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@library.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Member Users
            var members = new List<(string Email, string FullName, string Password)>
            {
                ("member1@library.com", "Nguyễn Văn A", "Member@123"),
                ("member2@library.com", "Trần Thị B", "Member@123")
            };

            foreach (var (email, fullName, password) in members)
            {
                var memberUser = await userManager.FindByEmailAsync(email);
                if (memberUser == null)
                {
                    memberUser = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = fullName,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(memberUser, password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(memberUser, "Member");
                    }
                }
            }

            // 4. Seed Categories
            if (!await context.Categories.AnyAsync())
            {
                var itCategory = new Category { Name = "Công nghệ thông tin" };
                var econCategory = new Category { Name = "Kinh tế - Quản trị" };
                var scienceCategory = new Category { Name = "Khoa học - Kỹ thuật" };
                var literatureCategory = new Category { Name = "Văn học - Nghệ thuật" };

                await context.Categories.AddRangeAsync(itCategory, econCategory, scienceCategory, literatureCategory);
                await context.SaveChangesAsync();

                // 5. Seed Books
                var books = new List<Book>
                {
                    // Công nghệ thông tin
                    new Book
                    {
                        Title = "Clean Code: A Handbook of Agile Software Craftsmanship",
                        Author = "Robert C. Martin",
                        Price = 250000,
                        Category = itCategory,
                        TotalCopies = 5,
                        AvailableCopies = 5
                    },
                    new Book
                    {
                        Title = "Design Patterns: Elements of Reusable Object-Oriented Software",
                        Author = "Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides",
                        Price = 320000,
                        Category = itCategory,
                        TotalCopies = 4,
                        AvailableCopies = 4
                    },
                    new Book
                    {
                        Title = "CLR via C# (4th Edition)",
                        Author = "Jeffrey Richter",
                        Price = 450000,
                        Category = itCategory,
                        TotalCopies = 3,
                        AvailableCopies = 3
                    },
                    new Book
                    {
                        Title = "ASP.NET Core in Action",
                        Author = "Andrew Lock",
                        Price = 380000,
                        Category = itCategory,
                        TotalCopies = 5,
                        AvailableCopies = 5
                    },

                    // Kinh tế - Quản trị
                    new Book
                    {
                        Title = "Đắc Nhân Tâm (How to Win Friends and Influence People)",
                        Author = "Dale Carnegie",
                        Price = 90000,
                        Category = econCategory,
                        TotalCopies = 10,
                        AvailableCopies = 10
                    },
                    new Book
                    {
                        Title = "Cha Giàu Cha Nghèo (Rich Dad Poor Dad)",
                        Author = "Robert T. Kiyosaki",
                        Price = 120000,
                        Category = econCategory,
                        TotalCopies = 6,
                        AvailableCopies = 6
                    },
                    new Book
                    {
                        Title = "Kinh Tế Học Vi Mô Cơ Bản",
                        Author = "N. Gregory Mankiw",
                        Price = 210000,
                        Category = econCategory,
                        TotalCopies = 4,
                        AvailableCopies = 4
                    },

                    // Khoa học - Kỹ thuật
                    new Book
                    {
                        Title = "Lược Sử Thời Gian (A Brief History of Time)",
                        Author = "Stephen Hawking",
                        Price = 115000,
                        Category = scienceCategory,
                        TotalCopies = 5,
                        AvailableCopies = 5
                    },
                    new Book
                    {
                        Title = "Vũ Trụ Trong Vỏ Hạt Dẻ",
                        Author = "Stephen Hawking",
                        Price = 135000,
                        Category = scienceCategory,
                        TotalCopies = 3,
                        AvailableCopies = 3
                    },

                    // Văn học - Nghệ thuật
                    new Book
                    {
                        Title = "Nhà Giả Kim (The Alchemist)",
                        Author = "Paulo Coelho",
                        Price = 85000,
                        Category = literatureCategory,
                        TotalCopies = 8,
                        AvailableCopies = 8
                    },
                    new Book
                    {
                        Title = "Dế Mèn Phiêu Lưu Ký",
                        Author = "Tô Hoài",
                        Price = 65000,
                        Category = literatureCategory,
                        TotalCopies = 7,
                        AvailableCopies = 7
                    },
                    new Book
                    {
                        Title = "Ông Già Và Biển Cả (The Old Man and the Sea)",
                        Author = "Ernest Hemingway",
                        Price = 75000,
                        Category = literatureCategory,
                        TotalCopies = 4,
                        AvailableCopies = 4
                    }
                };

                await context.Books.AddRangeAsync(books);
                await context.SaveChangesAsync();
            }
        }
    }
}
