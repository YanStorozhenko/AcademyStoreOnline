using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Models;

namespace OnlineStore.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
            
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            // Create roles
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }
            
            // Create admin user
            var adminEmail = "admin@store.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            
            // Create categories if they don't exist
            if (!context.Categories.Any())
            {
                var categories = new[]
                {
                    new Category { Name = "Электроника" },
                    new Category { Name = "Одежда" },
                    new Category { Name = "Книги" },
                    new Category { Name = "Спорт" },
                    new Category { Name = "Дом и сад" }
                };
                
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }
            
            // Create sample products if they don't exist
            if (!context.Products.Any())
            {
                var categories = context.Categories.ToList();
                var electronics = categories.First(c => c.Name == "Электроника");
                var books = categories.First(c => c.Name == "Книги");
                var sports = categories.First(c => c.Name == "Спорт");
                
                var products = new[]
                {
                    new Product
                    {
                        Name = "Смартфон Samsung Galaxy S23",
                        Description = "Мощный смартфон с отличной камерой",
                        Price = 79999.99m,
                        CategoryId = electronics.Id,
                        Stock = 50,
                        ImageUrl = "/images/placeholder-product.jpg"
                    },
                    new Product
                    {
                        Name = "Ноутбук ASUS ROG Strix",
                        Description = "Игровой ноутбук с RTX 4060",
                        Price = 129999.99m,
                        CategoryId = electronics.Id,
                        Stock = 25,
                        ImageUrl = "/images/placeholder-product.jpg"
                    },
                    new Product
                    {
                        Name = "Книга 'C# для начинающих'",
                        Description = "Полное руководство по программированию на C#",
                        Price = 2499.99m,
                        CategoryId = books.Id,
                        Stock = 100,
                        ImageUrl = "/images/placeholder-product.jpg"
                    },
                    new Product
                    {
                        Name = "Наушники Sony WH-1000XM5",
                        Description = "Беспроводные наушники с шумоподавлением",
                        Price = 35999.99m,
                        CategoryId = electronics.Id,
                        Stock = 30,
                        ImageUrl = "/images/placeholder-product.jpg"
                    },
                    new Product
                    {
                        Name = "Футбольный мяч Adidas",
                        Description = "Официальный мяч для профессиональных матчей",
                        Price = 4999.99m,
                        CategoryId = sports.Id,
                        Stock = 75,
                        ImageUrl = "/images/placeholder-product.jpg"
                    },
                    new Product
                    {
                        Name = "Фитнес-браслет Xiaomi Mi Band 7",
                        Description = "Умный браслет с отслеживанием сна и активности",
                        Price = 3499.99m,
                        CategoryId = electronics.Id,
                        Stock = 150,
                        ImageUrl = "/images/placeholder-product.jpg"
                    }
                };
                
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
        }
    }
}