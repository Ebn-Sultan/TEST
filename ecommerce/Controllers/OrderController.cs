using ecommerce.Models;
using ecommerce.Services;
using ecommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ecommerce.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService orderService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ICartItemService cartItemService;
        private readonly ICartService cartService;
        private readonly IOrderItemService orderItemService;
        private readonly IProductService productService;
        private readonly IShipmentService shipmentService;
        private readonly ICategoryService categoryService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService,
            UserManager<ApplicationUser> userManager,
            ICartService cartService,
            ICartItemService cartItemService,
            IOrderItemService orderItemService,
            IProductService productService,
            IShipmentService shipmentService,
            ICategoryService categoryService,
            ILogger<OrderController> logger)
        {
            this.orderService = orderService;
            this.userManager = userManager;
            this.cartItemService = cartItemService;
            this.cartService = cartService;
            this.orderItemService = orderItemService;
            this.productService = productService;
            this.shipmentService = shipmentService;
            this.categoryService = categoryService;
            this._logger = logger;
        }

        [HttpGet]
        public IActionResult GetAll(string? include = null)
        {
            List<Order> orders = orderService.GetAll(include);
            return View(orders);
        }

        [HttpGet]
        public IActionResult Get(int id)
        {
            Order order = orderService.Get(id);
            if (order != null)
            {
                return View(order);
            }
            return RedirectToAction("GetAll");
        }

        [HttpGet]
        public IActionResult Get(Func<Order, bool> where)
        {
            List<Order> orders = orderService.Get(where);
            return View(orders);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Insert()
        {
            /// TODO : continue from here make the VM and test the view
            /// Note : check Saeed for the register and login pages
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Insert(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    orderService.Insert(order);
                    orderService.Save();
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to insert order: " + ex.Message);
                    return View(order);
                }
            }
            return View(order);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            Order order = orderService.Get(id);
            if (order != null)
            {
                return View(order);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    orderService.Update(order);
                    orderService.Save();
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to update order: " + ex.Message);
                    return View(order);
                }
            }
            return View(order);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            Order order = orderService.Get(id);
            if (order != null)
            {
                return View(order);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(Order order)
        {
            try
            {
                orderService.Delete(order);
                orderService.Save();
                return RedirectToAction("GetAll");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to delete order: " + ex.Message);
                return View(order);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> checkout(int CartId, string UserId)
        {
            ViewData["AllProductsNames"] = productService.GetAll().Select(c => c.Name).ToList();

            ApplicationUser user = await userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            List<CartItem> items = cartItemService.Get(i => i.CartId == CartId);
            if (!items.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                Order order = new Order
                {
                    ApplicationUserId = user.Id,
                    OrderDate = DateTime.Now,
                    OrderItems = new List<OrderItem>()
                };

                foreach (var item in items)
                {
                    Product product = productService.Get(item.ProductId);
                    if (product == null || product.Quantity < item.Quantity)
                    {
                        ModelState.AddModelError("", $"Product {item.ProductId} is out of stock or insufficient quantity.");
                        return RedirectToAction("Index", "Home");
                    }
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    });
                }

                HttpContext.Session.SetString("order", JsonSerializer.Serialize(order));
                HttpContext.Session.SetString("uId", UserId);
                HttpContext.Session.SetInt32("cId", CartId);

                Shipment shipment = new Shipment { Date = DateTime.Now.AddDays(3) };
                CheckoutViewModel checkoutVM = new CheckoutViewModel
                {
                    Date = shipment.Date,
                    Categories = categoryService.GetAll(),
                    Cart = cartService.Get(CartId) ?? new Cart { CartItems = new List<CartItem>() }
                };

                decimal total = 0;
                foreach (OrderItem item in order.OrderItems)
                {
                    item.Product = productService.Get(item.ProductId);
                    if (item.Product != null)
                    {
                        total += item.Product.Price * item.Quantity;
                    }
                }
                ViewBag.order = order;
                ViewBag.total = total;

                return View(checkoutVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout GET");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult checkout(CheckoutViewModel checkoutVM)
        {
            ViewData["AllProductsNames"] = productService.GetAll().Select(c => c.Name).ToList();

            if (ModelState.IsValid)
            {
                try
                {
                    string orderJson = HttpContext.Session.GetString("order");
                    if (string.IsNullOrEmpty(orderJson))
                    {
                        ModelState.AddModelError("", "Order data is missing.");
                        return View(checkoutVM);
                    }

                    Order orderDeserialized = JsonSerializer.Deserialize<Order>(orderJson);
                    Order order = new Order
                    {
                        OrderDate = orderDeserialized.OrderDate,
                        ApplicationUserId = orderDeserialized.ApplicationUserId
                    };

                    orderService.Insert(order);
                    orderService.Save();

                    foreach (OrderItem item in orderDeserialized.OrderItems)
                    {
                        Product product = productService.Get(item.ProductId);
                        if (product == null || product.Quantity < item.Quantity)
                        {
                            ModelState.AddModelError("", $"Product {item.ProductId} is out of stock or insufficient quantity.");
                            return View(checkoutVM);
                        }
                        product.Quantity -= item.Quantity;
                        item.OrderId = order.Id;
                        orderItemService.Insert(item);
                        productService.Update(product);
                    }

                    orderItemService.Save();
                    productService.Save();

                    Shipment shipment = new Shipment
                    {
                        Address = checkoutVM.Address,
                        City = checkoutVM.City,
                        Region = checkoutVM.Region,
                        PostalCode = checkoutVM.PostalCode,
                        Country = checkoutVM.Country,
                        OrderId = order.Id,
                        UserId = orderDeserialized.ApplicationUserId,
                        Date = checkoutVM.Date
                    };
                    shipmentService.Insert(shipment);
                    shipmentService.Save();

                    order.ShipmentId = shipment.Id;
                    orderService.Update(order);
                    orderService.Save();

                    // Clear cart items
                    int? sessionCartId = HttpContext.Session.GetInt32("cId");
                    if (sessionCartId.HasValue)
                    {
                        var cartItems = cartItemService.Get(i => i.CartId == sessionCartId.Value);
                        foreach (var item in cartItems)
                        {
                            cartItemService.Delete(item);
                        }
                        cartItemService.Save();
                    }

                    HttpContext.Session.Remove("order");
                    HttpContext.Session.Remove("uId");
                    HttpContext.Session.Remove("cId");

                    checkoutVM.Order = null;
                    return View("PlaceOrder");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during checkout POST");
                    ModelState.AddModelError("", "Failed to process order: " + ex.Message);
                    return View(checkoutVM);
                }
            }

            string? userId = HttpContext.Session.GetString("uId");
            int? cartId = HttpContext.Session.GetInt32("cId");
            return RedirectToAction("checkout", new { CartId = cartId, UserId = userId });
        }
    }
}