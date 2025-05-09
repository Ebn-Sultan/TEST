using ecommerce.Models;
using ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Controllers
{
    public class OrderItemController : Controller
    {
        private readonly IOrderItemService orderItemService;

        public OrderItemController(IOrderItemService orderItemService)
        {
            this.orderItemService = orderItemService;
        }

        [HttpGet]
        public IActionResult GetAll(string? include = null)
        {
            List<OrderItem> orderItems = orderItemService.GetAll(include);
            return View(orderItems);
        }

        [HttpGet]
        public IActionResult Get(int id)
        {
            OrderItem orderItem = orderItemService.Get(id);
            if (orderItem != null)
            {
                return View(orderItem);
            }
            return RedirectToAction("GetAll");
        }

        [HttpGet]
        public IActionResult Get(Func<OrderItem, bool> where)
        {
            List<OrderItem> orderItems = orderItemService.Get(where);
            return View(orderItems);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Insert()
        {
            /// TODO: Continue from here, create a ViewModel and test the view
            /// Note: Check with Saeed for the register and login pages
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Insert(OrderItem orderItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    orderItemService.Insert(orderItem);
                    orderItemService.Save();
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to insert order item: " + ex.Message);
                    return View(orderItem);
                }
            }
            return View(orderItem);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            OrderItem orderItem = orderItemService.Get(id);
            if (orderItem != null)
            {
                return View(orderItem);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(OrderItem orderItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    orderItemService.Update(orderItem);
                    orderItemService.Save();
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to update order item: " + ex.Message);
                    return View(orderItem);
                }
            }
            return View(orderItem);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            OrderItem orderItem = orderItemService.Get(id);
            if (orderItem != null)
            {
                return View(orderItem);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(OrderItem orderItem)
        {
            try
            {
                orderItemService.Delete(orderItem);
                orderItemService.Save();
                return RedirectToAction("GetAll");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to delete order item: " + ex.Message);
                return View(orderItem);
            }
        }
    }
}