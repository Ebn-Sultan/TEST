using ecommerce.Hubs;
using ecommerce.Models;
using ecommerce.Repository;
using ecommerce.Services;
using ecommerce.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //Add comment
            builder.Services.AddSignalR();
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            //inject the context
            builder.Services.AddDbContext<Context>(
                options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("cs"));
                });

            //register Model.
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();

            builder.Services.AddScoped<ICommentService, CommentService>();

            //AbdElraheem
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();
            builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            builder.Services.AddScoped<IOrderItemService, OrderItemService>();

            // omar : registering order repo
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();

            // omar : registering orderservice
            builder.Services.AddScoped<IOrderService, OrderService>();

            // omar : registering ProductService
            builder.Services.AddScoped<IProductService, ProductService>();

            // omar : registering CategoryService
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();

            builder.Services.AddScoped<IShipmentService, ShipmentService>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
            builder.Services.AddScoped<ICartItemService, CartItemService>();

            // saeed : register shipment using generic repository
            builder.Services.AddScoped<IRepository<Shipment>, Repository<Shipment>>();

            //register the identityuser 
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(
                options =>
                {
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireDigit = false;
                }
                ).AddEntityFrameworkStores<Context>().AddDefaultTokenProviders();

            // saeed : mail configuration
            builder.Services.Configure<MailSettings>
                (builder.Configuration.GetSection("MailSettings"));

            builder.Services.AddTransient<IMailService, MailService>();

            builder.Services.AddSession();

            // omar : registering cart and cartItems
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<ICartItemService, CartItemService>();

            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();

            // Add logging
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            // saeed
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            //Comment
            app.MapHub<CommentHub>("/CommentHub");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Initialize roles and default admin user
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                string[] roleNames = { "Admin", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Register default admin user
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                string adminEmail = "sabdosaber13@gmail.com";
                string adminPassword = "Admin@123";

                var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
                if (existingAdmin == null)
                {
                    var adminUser = new ApplicationUser
                    {
                        UserName = "Abdosaber",
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                    else
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        foreach (var error in result.Errors)
                        {
                            logger.LogError($"Error creating admin user: {error.Description}");
                        }
                    }
                }
            }

            app.Run();
        }
    }
}