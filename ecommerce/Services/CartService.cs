using ecommerce.Models;
using ecommerce.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ecommerce.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository cartRepository;
        private readonly ILogger<CartService> _logger;

        public CartService(ICartRepository cartRepository, ILogger<CartService> logger)
        {
            this.cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Cart> GetAll(string include = null)
        {
            try
            {
                _logger.LogInformation("Retrieving all carts with include: {Include}", include ?? "none");
                return cartRepository.GetAll(include);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve carts");
                throw;
            }
        }

        public Cart Get(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving cart with ID: {Id}", id);
                return cartRepository.Get(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve cart with ID: {Id}", id);
                throw;
            }
        }

        public List<Cart> Get(Func<Cart, bool> where)
        {
            try
            {
                _logger.LogInformation("Retrieving carts with condition");
                return cartRepository.Get(where);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve carts with condition");
                throw;
            }
        }

        public void Insert(Cart cart)
        {
            try
            {
                if (cart == null)
                {
                    _logger.LogWarning("Attempted to insert null cart");
                    throw new ArgumentNullException(nameof(cart));
                }

                _logger.LogInformation("Inserting cart");
                cartRepository.Insert(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert cart");
                throw;
            }
        }

        public void Update(Cart updatedCart)
        {
            try
            {
                if (updatedCart == null)
                {
                    _logger.LogWarning("Attempted to update null cart");
                    throw new ArgumentNullException(nameof(updatedCart));
                }

                Cart cart = Get(updatedCart.Id);
                if (cart == null)
                {
                    _logger.LogWarning("Cart not found for update: {Id}", updatedCart.Id);
                    throw new InvalidOperationException($"Cart with ID {updatedCart.Id} not found.");
                }

                // Update fields
                cart.CartItems = updatedCart.CartItems ?? new List<CartItem>();

                _logger.LogInformation("Updating cart ID: {Id}", cart.Id);
                cartRepository.Update(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update cart ID: {Id}", updatedCart?.Id);
                throw;
            }
        }

        public void Delete(Cart cart)
        {
            try
            {
                if (cart == null)
                {
                    _logger.LogWarning("Attempted to delete null cart");
                    throw new ArgumentNullException(nameof(cart));
                }

                _logger.LogInformation("Deleting cart ID: {Id}", cart.Id);
                cartRepository.Delete(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete cart ID: {Id}", cart?.Id);
                throw;
            }
        }

        public void Save()
        {
            try
            {
                _logger.LogInformation("Saving cart changes");
                cartRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save cart changes");
                throw;
            }
        }
    }
}