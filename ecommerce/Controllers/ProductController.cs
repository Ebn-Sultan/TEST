using ecommerce.Models;
using ecommerce.Services;
using ecommerce.ViewModels.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService productService;
        private readonly ICategoryService categoryService;
        private readonly ICartService cartService;
        private readonly ICartItemService cartItemService;
        private readonly ICommentService commentService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductController> _logger;

        private const int _pageSize = 6;

        public ProductController(IProductService productService, ICategoryService categoryService,
            ICartService cartService, ICartItemService cartItemService, ICommentService commentService,
            IWebHostEnvironment webHostEnvironment, ILogger<ProductController> logger)
        {
            this.productService = productService;
            this.categoryService = categoryService;
            this.cartService = cartService;
            this.cartItemService = cartItemService;
            this.commentService = commentService;
            this._webHostEnvironment = webHostEnvironment;
            this._logger = logger;
        }

        [HttpGet]
        public IActionResult GetAll(int page = 1, int pageSize = _pageSize)
        {
            try
            {
                int skipStep = (page - 1) * pageSize;
                List<Product> paginatedProducts = productService.GetPageList(skipStep, pageSize);
                int productsCount = productService.GetAll().Count();

                ViewData["TotalPages"] = Math.Ceiling(productsCount / (double)pageSize);
                ViewData["AllProductsNames"] = paginatedProducts.Select(c => c.Name).ToList();
                ViewBag.PageSize = pageSize;
                ViewBag.TotalProductsNumber = productsCount;

                string userIdClaim = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                ViewBag.UserId = userIdClaim;

                List<Cart> carts = cartService.GetAll();
                Products_With_CategoriesVM products_CategoriesVM;

                if (carts == null || carts.Count == 0)
                {
                    Cart cart = new Cart { CartItems = new List<CartItem>() };
                    cartService.Insert(cart);
                    cartService.Save();
                    _logger.LogInformation("New cart created");
                    products_CategoriesVM = new Products_With_CategoriesVM
                    {
                        Products = paginatedProducts,
                        Categories = categoryService.GetAll(),
                        Cart = cart
                    };
                }
                else
                {
                    products_CategoriesVM = new Products_With_CategoriesVM
                    {
                        Products = paginatedProducts,
                        Categories = categoryService.GetAll(),
                        Cart = cartService.GetAll("CartItems").FirstOrDefault() ?? new Cart { CartItems = new List<CartItem>() }
                    };
                }

                return View(products_CategoriesVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load products page");
                TempData["Error"] = "An error occurred while loading products.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult GetAllPartial(int[] catedIds, int page = 1, int pageSize = _pageSize)
        {
            try
            {
                if (catedIds == null || !catedIds.Any())
                {
                    catedIds = categoryService.GetAll().Select(c => c.Id).ToArray();
                }

                int skipStep = (page - 1) * pageSize;
                List<Product> paginatedProducts = productService.GetPageList(skipStep, pageSize)
                    .Where(p => catedIds.Contains(p.CategoryId)).ToList();
                int productsCount = productService.GetAll().Where(p => catedIds.Contains(p.CategoryId)).Count();

                ViewData["TotalPages"] = Math.Ceiling(productsCount / (double)pageSize);
                ViewData["AllProductsNames"] = paginatedProducts.Select(c => c.Name).ToList();

                Products_With_CategoriesVM products_CategoriesVM = new Products_With_CategoriesVM
                {
                    Products = paginatedProducts,
                    Categories = categoryService.GetAll()
                };

                return PartialView("_ProductsPartial", products_CategoriesVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load partial products view");
                TempData["Error"] = "An error occurred while loading filtered products.";
                return PartialView("_ProductsPartial", new Products_With_CategoriesVM());
            }
        }

        [HttpGet]
        public IActionResult GetAllFiltered(int minPrice, int maxPrice, int[] categIds, int page = 1, int pageSize = _pageSize)
        {
            try
            {
                if (categIds == null || !categIds.Any())
                {
                    categIds = categoryService.GetAll().Select(c => c.Id).ToArray();
                }

                int skipStep = (page - 1) * pageSize;
                List<Product> filteredProducts = productService.GetAll()
                    .Where(p => categIds.Contains(p.CategoryId))
                    .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
                    .ToList();
                List<Product> paginatedProducts = filteredProducts.Skip(skipStep).Take(pageSize).ToList();
                int productsCount = filteredProducts.Count();

                ViewData["TotalPages"] = Math.Ceiling(productsCount / (double)pageSize);
                ViewData["AllProductsNames"] = paginatedProducts.Select(c => c.Name).ToList();

                Products_With_CategoriesVM products_CategoriesVM = new Products_With_CategoriesVM
                {
                    Products = paginatedProducts,
                    Categories = categoryService.GetAll()
                };

                return PartialView("_ProductsPartial", products_CategoriesVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load filtered products view");
                TempData["Error"] = "An error occurred while loading filtered products.";
                return PartialView("_ProductsPartial", new Products_With_CategoriesVM());
            }
        }

        public IActionResult Search(string searchProdName)
        {
            try
            {
                if (string.IsNullOrEmpty(searchProdName))
                {
                    return RedirectToAction("GetAll");
                }

                List<Product> searchedProducts = productService.GetAll()
                    .Where(p => p.Name.Contains(searchProdName, StringComparison.OrdinalIgnoreCase)).ToList();

                if (searchedProducts.Any())
                {
                    int id = searchedProducts.First().Id;
                    return RedirectToAction("Details", "Product", new { id });
                }

                TempData["Error"] = "No products found matching the search term.";
                return RedirectToAction("GetAll");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search products: {SearchTerm}", searchProdName);
                TempData["Error"] = "An error occurred while searching for products.";
                return RedirectToAction("GetAll");
            }
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            try
            {
                Product productDB = productService.Get(id);
                if (productDB == null)
                {
                    _logger.LogWarning("Product not found: {ProductId}", id);
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("GetAll");
                }

                Category prodCateg = categoryService.Get(productDB.CategoryId);
                string userIdClaim = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                ViewBag.UserId = userIdClaim;
                ViewBag.Comments = commentService.GetComments(c => c.ProductId == id);

                Cart cart = cartService.GetAll("CartItems").FirstOrDefault() ?? new Cart { CartItems = new List<CartItem>() };

                Product_With_RelatedProducts prodVM = new Product_With_RelatedProducts
                {
                    Id = productDB.Id,
                    Name = productDB.Name,
                    Description = productDB.Description,
                    Price = productDB.Price,
                    Quantity = productDB.Quantity,
                    ImageUrl = productDB.ImageUrl,
                    Rating = productDB.Rating,
                    Color = productDB.Color,
                    CategoryId = productDB.CategoryId,
                    Category = prodCateg,
                    CategoryName = prodCateg?.Name,
                    Comments = productDB.Comments,
                    RealtedProducts = productService.Get(p => p.CategoryId == productDB.CategoryId && p.Id != id).Take(3).ToList(),
                    Cart = cart,
                    Categories = categoryService.GetAll()
                };

                return View("Get", prodVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load product details: {ProductId}", id);
                TempData["Error"] = "An error occurred while loading product details.";
                return RedirectToAction("GetAll");
            }
        }

        [HttpGet]
        public IActionResult Get(Func<Product, bool> where)
        {
            try
            {
                List<Product> products = productService.Get(where);
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load products with condition");
                TempData["Error"] = "An error occurred while loading products.";
                return RedirectToAction("GetAll");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Insert()
        {
            ProductWithListOfCatesViewModel product = new ProductWithListOfCatesViewModel
            {
                categories = categoryService.GetAll()
            };
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Insert(ProductWithListOfCatesViewModel product)
        {
            try
            {
                if (product.image != null && product.image.Length > 0)
                {
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "img");
                    string imageName = Guid.NewGuid().ToString() + "_" + product.image.FileName;
                    string filePath = Path.Combine(uploadPath, imageName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        product.image.CopyTo(fileStream);
                    }
                    product.ImageUrl = imageName;
                }

                if (ModelState.IsValid)
                {
                    productService.Insert(product);
                    productService.Save();
                    TempData["Success"] = "Product added successfully!";
                    return RedirectToAction("products", "Dashbourd");
                }

                product.categories = categoryService.GetAll();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert product");
                ModelState.AddModelError("", "Failed to insert product: " + ex.Message);
                product.categories = categoryService.GetAll();
                return View(product);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            ProductWithListOfCatesViewModel product = productService.GetViewModel(id);
            if (product != null)
            {
                product.categories = categoryService.GetAll();
                return View(product);
            }
            _logger.LogWarning("Product not found for update: {ProductId}", id);
            TempData["Error"] = "Product not found.";
            return RedirectToAction("products", "Dashbourd");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(ProductWithListOfCatesViewModel product)
        {
            try
            {
                if (product.image != null && product.image.Length > 0)
                {
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "img");
                    string imageName = Guid.NewGuid().ToString() + "_" + product.image.FileName;
                    string filePath = Path.Combine(uploadPath, imageName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        product.image.CopyTo(fileStream);
                    }
                    product.ImageUrl = imageName;
                }

                if (ModelState.IsValid)
                {
                    productService.Update(product);
                    productService.Save();
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction("products", "Dashbourd");
                }

                product.categories = categoryService.GetAll();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product: {ProductId}", product.Id);
                ModelState.AddModelError("", "Failed to update product: " + ex.Message);
                product.categories = categoryService.GetAll();
                return View(product);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            Product product = productService.Get(id);
            if (product != null)
            {
                return View(product);
            }
            _logger.LogWarning("Product not found for deletion: {ProductId}", id);
            TempData["Error"] = "Product not found.";
            return RedirectToAction("products", "Dashbourd");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(Product product)
        {
            try
            {
                productService.Delete(product);
                productService.Save();
                TempData["Success"] = "Product deleted successfully!";
                return RedirectToAction("products", "Dashbourd");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product: {ProductId}", product.Id);
                ModelState.AddModelError("", "Failed to delete product: " + ex.Message);
                return View(product);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult AddtoCart(int id, int quantity = 1)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Unauthenticated user attempted to add to cart: {ProductId}", id);
                    TempData["Error"] = "Please log in to add products to the cart.";
                    return RedirectToAction("Login", "Account");
                }

                Product product = productService.Get(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found: {ProductId}", id);
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Details", new { id });
                }

                if (product.Quantity < quantity)
                {
                    _logger.LogWarning("Insufficient product quantity: {ProductId}, Requested: {Quantity}, Available: {Available}",
                        id, quantity, product.Quantity);
                    TempData["Error"] = "Insufficient product quantity.";
                    return RedirectToAction("Details", new { id });
                }

                Cart cart = cartService.GetAll("CartItems").FirstOrDefault();
                if (cart == null)
                {
                    cart = new Cart { CartItems = new List<CartItem>() };
                    cartService.Insert(cart);
                    cartService.Save();
                    _logger.LogInformation("New cart created: {CartId}", cart.Id);
                }

                CartItem existedItem = cart.CartItems?.FirstOrDefault(c => c.ProductId == id);
                if (existedItem != null)
                {
                    existedItem.Quantity += quantity;
                    cartItemService.Update(existedItem);
                }
                else
                {
                    CartItem cartItem = new CartItem
                    {
                        Quantity = quantity,
                        ProductId = id,
                        Product = product,
                        CartId = cart.Id,
                        Cart = cart
                    };
                    cartItemService.Insert(cartItem);
                }

                cartItemService.Save();
                _logger.LogInformation("Product added to cart: {ProductId}, Cart: {CartId}", id, cart.Id);
                TempData["Success"] = "Product added to cart successfully!";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add product to cart: {ProductId}", id);
                TempData["Error"] = "An error occurred while adding the product to the cart.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}