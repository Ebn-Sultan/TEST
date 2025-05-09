using ecommerce.Models;
using ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ecommerce.Controllers
{
    [Authorize]
    public class ShipmentController : Controller
    {
        private readonly IShipmentService shipmentService;
        private readonly IOrderItemService orderItemService;
        private readonly IOrderService orderService;
        private readonly IProductService productService;
        private readonly ILogger<ShipmentController> _logger;

        public ShipmentController(IShipmentService shipmentService, IOrderItemService orderItemService,
            IOrderService orderService, IProductService productService, ILogger<ShipmentController> logger)
        {
            this.shipmentService = shipmentService;
            this.orderItemService = orderItemService;
            this.orderService = orderService;
            this.productService = productService;
            this._logger = logger;
        }

        [HttpGet]
        public IActionResult GetAll(string? include = null)
        {
            List<Shipment> shipmentList = shipmentService.GetAll(include);
            return View("GetAll", shipmentList);
        }

        [HttpGet]
        public IActionResult Get(int id)
        {
            Shipment shipment = shipmentService.Get(id);
            if (shipment != null)
            {
                return View(shipment);
            }
            return RedirectToAction("GetAll");
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
        public IActionResult Insert(Shipment shipment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    shipmentService.Insert(shipment);
                    shipmentService.Save();
                    _logger.LogInformation("Shipment inserted successfully: {ShipmentId}", shipment.Id);
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to insert shipment: {ShipmentId}", shipment.Id);
                    ModelState.AddModelError("", "Failed to insert shipment: " + ex.Message);
                    return View(shipment);
                }
            }
            return View(shipment);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            Shipment shipment = shipmentService.Get(id);
            if (shipment != null)
            {
                return View(shipment);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(Shipment shipment)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    shipmentService.Update(shipment);
                    shipmentService.Save();
                    _logger.LogInformation("Shipment updated successfully: {ShipmentId}", shipment.Id);
                    return RedirectToAction("GetAll");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update shipment: {ShipmentId}", shipment.Id);
                    ModelState.AddModelError("", "Failed to update shipment: " + ex.Message);
                    return View(shipment);
                }
            }
            return View(shipment);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            Shipment shipment = shipmentService.Get(id);
            if (shipment != null)
            {
                return View(shipment);
            }
            return RedirectToAction("GetAll");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(Shipment shipment)
        {
            try
            {
                shipmentService.Delete(shipment);
                shipmentService.Save();
                _logger.LogInformation("Shipment deleted successfully: {ShipmentId}", shipment.Id);
                return RedirectToAction("GetAll");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete shipment: {ShipmentId}", shipment.Id);
                ModelState.AddModelError("", "Failed to delete shipment: " + ex.Message);
                return View(shipment);
            }
        }

        [HttpPost]
        public IActionResult PlaceShipment(Shipment shipment)
        {
            try
            {
                var orderJson = HttpContext.Session.GetString("order");
                if (string.IsNullOrEmpty(orderJson))
                {
                    _logger.LogWarning("No order found in session for shipment placement");
                    ModelState.AddModelError("", "No order found in session.");
                    return View(shipment);
                }

                Order orderDeserialized;
                try
                {
                    orderDeserialized = JsonSerializer.Deserialize<Order>(orderJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize order from session");
                    ModelState.AddModelError("", "Invalid order data in session.");
                    return View(shipment);
                }

                var order = new Order
                {
                    OrderDate = orderDeserialized.OrderDate,
                    ApplicationUserId = orderDeserialized.ApplicationUserId
                };
                orderService.Insert(order);
                orderService.Save();

                shipment.OrderId = order.Id;
                shipment.Date = DateTime.Now.AddDays(3);
                shipmentService.Insert(shipment);

                order.ShipmentId = shipment.Id;
                orderService.Update(order);

                shipmentService.Save();
                orderService.Save();

                HttpContext.Session.Remove("order");
                _logger.LogInformation("Shipment placed successfully: {ShipmentId}, Order: {OrderId}", shipment.Id, order.Id);
                return View(shipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to place shipment");
                ModelState.AddModelError("", "Failed to place shipment: " + ex.Message);
                return View(shipment);
            }
        }

        // saeed: get shipment products "return partial view"
        [HttpGet]
        public IActionResult GetShipmentProducts(int id)
        {
            try
            {
                Shipment shipment = shipmentService.Get(id);
                if (shipment == null)
                {
                    _logger.LogWarning("Shipment not found: {ShipmentId}", id);
                    return NotFound();
                }

                List<Product> shipmentProducts = new List<Product>();
                List<OrderItem> orderItems = orderItemService.Get(oi => oi.OrderId == shipment.OrderId);

                decimal totalOrderPrice = 0;
                foreach (OrderItem orderItem in orderItems)
                {
                    Product product = productService.Get(orderItem.ProductId);
                    if (product != null)
                    {
                        product.Quantity = orderItem.Quantity;
                        shipmentProducts.Add(product);
                        totalOrderPrice += (product.Quantity * product.Price);
                    }
                }
                ViewBag.TotalOrderPrice = totalOrderPrice;

                return PartialView("_GetShipmentProductsPartial", shipmentProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get shipment products: {ShipmentId}", id);
                return StatusCode(500, "An error occurred while retrieving shipment products.");
            }
        }
    }
}