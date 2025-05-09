using ecommerce.Models;
using ecommerce.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ecommerce.Services
{
    public class CartItemService : ICartItemService
    {
        private readonly ICartItemRepository cartItemRepository;
        private readonly ILogger<CartItemService> _logger;

        public CartItemService(ICartItemRepository cartItemRepository, ILogger<CartItemService> logger)
        {
            this.cartItemRepository = cartItemRepository ?? throw new ArgumentNullException(nameof(cartItemRepository));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<CartItem> GetAll(string include = null)
        {
            try
            {
                _logger.LogInformation("Retrieving all cart items with include: {Include}", include ?? "none");
                return cartItemRepository.GetAll(include);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve cart items");
                throw;
            }
        }

        public CartItem Get(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving cart item with ID: {Id}", id);
                return cartItemRepository.Get(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve cart item with ID: {Id}", id);
                throw;
            }
        }

        public List<CartItem> Get(Func<CartItem, bool> where)
        {
            try
            {
                _logger.LogInformation("Retrieving cart items with condition");
                return cartItemRepository.Get(where);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve cart items with condition");
                throw;
            }
        }

        public void Insert(CartItem cartItem)
        {
            try
            {
                if (cartItem == null)
                {
                    _logger.LogWarning("Attempted to insert null cart item");
                    throw new ArgumentNullException(nameof(cartItem));
                }

                _logger.LogInformation("Inserting cart item for Product ID: {ProductId}, Cart ID: {CartId}",
                    cartItem.ProductId, cartItem.CartId);
                cartItemRepository.Insert(cartItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert cart item for Product ID: {ProductId}", cartItem?.ProductId);
                throw;
            }
        }

        public void Update(CartItem updatedCartItem)
        {
            try
            {
                if (updatedCartItem == null)
                {
                    _logger.LogWarning("Attempted to update null cart item");
                    throw new ArgumentNullException(nameof(updatedCartItem));
                }

                CartItem cartItem = Get(updatedCartItem.Id);
                if (cartItem == null)
                {
                    _logger.LogWarning("Cart item not found for update: {Id}", updatedCartItem.Id);
                    throw new InvalidOperationException($"Cart item with ID {updatedCartItem.Id} not found.");
                }

                // Update fields
                cartItem.Quantity = updatedCartItem.Quantity;
                cartItem.ProductId = updatedCartItem.ProductId;
                cartItem.CartId = updatedCartItem.CartId;

                _logger.LogInformation("Updating cart item ID: {Id}, New Quantity: {Quantity}",
                    cartItem.Id, cartItem.Quantity);
                cartItemRepository.Update(cartItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update cart item ID: {Id}", updatedCartItem?.Id);
                throw;
            }
        }

        public void Delete(CartItem cartItem)
        {
            try
            {
                if (cartItem == null)
                {
                    _logger.LogWarning("Attempted to delete null cart item");
                    throw new ArgumentNullException(nameof(cartItem));
                }

                _logger.LogInformation("Deleting cart item ID: {Id}", cartItem.Id);
                cartItemRepository.Delete(cartItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete cart item ID: {Id}", cartItem?.Id);
                throw;
            }
        }

        public void Save()
        {
            try
            {
                _logger.LogInformation("Saving cart item changes");
                cartItemRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save cart item changes");
                throw;
            }
        }
    }
}