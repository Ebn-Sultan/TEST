using ecommerce.Models;
using ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService cartService;
        private readonly IProductService productService;
        private readonly ICartItemService cartItemService;

        public CartController(ICartService cartService, IProductService productService, ICartItemService cartItemService)
        {
            this.cartService = cartService;
            this.productService = productService;
            this.cartItemService = cartItemService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Cart> carts = cartService.GetAll();
            return View(carts);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            Cart cart = cartService.Get(id);
            if (cart != null)
            {
                return View(cart);
            }
            return RedirectToAction("GetAll");
        }

        [HttpGet]
        public IActionResult Get(Func<Cart, bool> where)
        {
            List<Cart> carts = cartService.Get(where);
            return View(carts);
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
        public IActionResult Insert(Cart cart)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    cartService.Insert(cart);
                    cartService.Save();
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to insert cart: " + ex.Message);
                    return View(cart);
                }
            }
            return View(cart);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            Cart cart = cartService.Get(id);
            if (cart != null)
            {
                return View(cart);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(Cart cart)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    cartService.Update(cart);
                    cartService.Save();
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to update cart: " + ex.Message);
                    return View(cart);
                }
            }
            return View(cart);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            Cart cart = cartService.Get(id);
            if (cart != null)
            {
                return View(cart);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(Cart cart)
        {
            try
            {
                cartService.Delete(cart);
                cartService.Save();
                return RedirectToAction("GetAll");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to delete cart: " + ex.Message);
                return View(cart);
            }
        }
    }
}