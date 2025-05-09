using ecommerce.Models;
using ecommerce.Services;
using ecommerce.ViewModels.Home;
using ecommerce.ViewModels.Product;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICartService cartService;
        private readonly IProductService productService;
        private readonly ICategoryService categoryService;

        public HomeController(ILogger<HomeController> logger, ICartService cartService,
            IProductService productService, ICategoryService categoryService)
        {
            _logger = logger;
            this.cartService = cartService;
            this.productService = productService;
            this.categoryService = categoryService;
        }

        public IActionResult Index()
        {
            try
            {
                /// TODO: Continue from here, verify if User-specific carts are needed
                /// Note: Check with Saeed for the register and login pages
                // Get products and categories
                List<Product> products = productService.GetAll("Category");
                ViewBag.AllProductsNames = products.Select(p => p.Name).ToList();

                // Get user ID if authenticated
                string userIdClaim = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                ViewBag.UserId = userIdClaim;

                // Get or create cart
                Cart cart = cartService.GetAll("CartItems").FirstOrDefault();
                if (cart == null)
                {
                    cart = new Cart { CartItems = new List<CartItem>() };
                    cartService.Insert(cart);
                    cartService.Save();
                    _logger.LogInformation("New cart created");
                }

                Prod_Cat_Cart_VM model = new Prod_Cat_Cart_VM
                {
                    Categories = categoryService.GetAll(),
                    Products = products,
                    Cart = cart
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load home page");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
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

        public IActionResult Search(string searchProdName)
        {
            if (string.IsNullOrEmpty(searchProdName))
            {
                return RedirectToAction("Index");
            }

            try
            {
                List<Product> searchedProducts = productService.GetAll()
                    .Where(p => p.Name.Contains(searchProdName, StringComparison.OrdinalIgnoreCase)).ToList();

                if (searchedProducts.Any())
                {
                    int id = searchedProducts.First().Id;
                    return RedirectToAction("Details", "Product", new { id });
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search products: {SearchTerm}", searchProdName);
                return RedirectToAction("Index");
            }
        }
    }
}