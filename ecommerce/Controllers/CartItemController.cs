using ecommerce.Models;
using ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    public class CartItemController : Controller
    {
        private readonly ICartItemService cartItemService;
        private readonly ICartService cartService;

        public CartItemController(ICartItemService cartItemService, ICartService cartService)
        {
            this.cartItemService = cartItemService;
            this.cartService = cartService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            // Get the user ID if logged in
            string userIdClaim = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            ViewBag.UserId = userIdClaim;

            List<CartItem> cartItems = cartItemService.GetAll("Product");
            return View(cartItems);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            CartItem cartItem = cartItemService.Get(id);
            if (cartItem != null)
            {
                return View("Get", cartItem);
            }
            return RedirectToAction("GetAll");
        }

        [HttpGet]
        public IActionResult Get(Func<CartItem, bool> where)
        {
            List<CartItem> cartItems = cartItemService.Get(where);
            return View(cartItems);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Insert()
        {
            /// TODO : continue from here make the VM and test the view
            /// Note : check Saeed for the register and login pages
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Insert(CartItem cartItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    cartItemService.Insert(cartItem);
                    cartItemService.Save();
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to insert cart item: " + ex.Message);
                    return View(cartItem);
                }
            }
            return View(cartItem);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Update(int id)
        {
            CartItem cartItem = cartItemService.Get(id);
            if (cartItem != null)
            {
                return View(cartItem);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Update(CartItem cartItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    cartItemService.Update(cartItem);
                    cartItemService.Save();
                    cartService.Save();
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to update cart item: " + ex.Message);
                    return View(cartItem);
                }
            }
            return View(cartItem);
        }
        //[HttpGet]
        ////[ValidateAntiForgeryToken]
        //[Authorize]
        //public IActionResult Delete(int id)
        //{
        //    var cartItem = cartItemService.Get(id);
        //    if (cartItem == null)
        //        return RedirectToAction("GetAll");

        //    return View(cartItem);
        //}

        //[HttpPost]
        ////[ValidateAntiForgeryToken]
        //[Authorize]
        //public IActionResult DeleteConfirmed(int id)
        //{
        //    var cartItem = cartItemService.Get(id);
        //    if (cartItem != null)
        //    {
        //        try
        //        {
        //            cartItemService.Delete(cartItem);
        //            cartItemService.Save();
        //            return RedirectToAction("GetAll", "CartItem");
        //        }
        //        catch (Exception ex)
        //        {
        //            ModelState.AddModelError("", "Failed to delete cart item: " + ex.Message);
        //            return View("Delete", cartItem);
        //        }
        //    }
        //    return RedirectToAction("GetAll", "CartItem");
        //}
        [HttpGet]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var cartItem = cartItemService.Get(id);
            if (cartItem == null)
                return RedirectToAction("GetAll");

            return View(cartItem);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult DeleteConfirmed(int id)
        {
            var cartItem = cartItemService.Get(id);
            if (cartItem != null)
            {
                try
                {
                    cartItemService.Delete(cartItem);
                    cartItemService.Save();
                    return RedirectToAction("GetAll", "CartItem");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to delete cart item: " + ex.Message);
                    return View("Delete", cartItem);
                }
            }
            return RedirectToAction("GetAll", "CartItem");
        }

    }
}