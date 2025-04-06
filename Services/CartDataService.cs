using Dapper;
using CardMaxxing.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;

namespace CardMaxxing.Services
{
    public class CartDataService : ICartDataService
    {
        private readonly IDbConnection _db;
        private readonly IProductDataService _productService;
        private readonly ILogger<CartDataService> _logger;
        private readonly TelemetryClient _telemetryClient;

        public CartDataService(
            IDbConnection db, 
            IProductDataService productService,
            ILogger<CartDataService> logger,
            TelemetryClient telemetryClient)
        {
            _db = db;
            _productService = productService;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        // Adds a product to the user's cart or increments its quantity if already present
        public async Task<bool> AddToCartAsync(string userId, string productId)
        {
            _logger.LogInformation("Adding product {ProductId} to cart for user {UserId}", productId, userId);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("AddToCart");
                
                string query = @"
                    INSERT INTO cart (UserID, ProductID, Quantity) 
                    VALUES (@UserID, @ProductID, 1) 
                    ON DUPLICATE KEY UPDATE Quantity = Quantity + 1;";

                int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId, ProductID = productId });
                
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Successfully added product {ProductId} to cart for user {UserId}", productId, userId);
                    _telemetryClient.TrackEvent("ProductAddedToCart", new Dictionary<string, string>
                    {
                        { "UserId", userId },
                        { "ProductId", productId }
                    });
                    return true;
                }
                
                _logger.LogWarning("Failed to add product {ProductId} to cart for user {UserId}", productId, userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart for user {UserId}", productId, userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "AddToCart" },
                    { "UserId", userId },
                    { "ProductId", productId }
                });
                throw;
            }
        }

        // Decrements quantity of a product in cart or removes it if quantity becomes zero
        public async Task<bool> RemoveFromCartAsync(string userId, string productId)
        {
            _logger.LogInformation("Removing product {ProductId} from cart for user {UserId}", productId, userId);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("RemoveFromCart");
                using var transaction = _db.BeginTransaction();
                
                string decrementQuery = @"
                    UPDATE cart SET Quantity = Quantity - 1 
                    WHERE UserID = @UserID AND ProductID = @ProductID AND Quantity > 1;";

                string deleteQuery = "DELETE FROM cart WHERE UserID = @UserID AND ProductID = @ProductID AND Quantity <= 1;";

                await _db.ExecuteAsync(decrementQuery, new { UserID = userId, ProductID = productId }, transaction);
                await _db.ExecuteAsync(deleteQuery, new { UserID = userId, ProductID = productId }, transaction);

                transaction.Commit();
                
                _logger.LogInformation("Successfully removed product {ProductId} from cart for user {UserId}", productId, userId);
                _telemetryClient.TrackEvent("ProductRemovedFromCart", new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "ProductId", productId }
                });
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product {ProductId} from cart for user {UserId}", productId, userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "RemoveFromCart" },
                    { "UserId", userId },
                    { "ProductId", productId }
                });
                return false;
            }
        }

        // Retrieves all items in the user's cart with their associated product details
        public async Task<List<OrderItemsModel>> GetCartItemsAsync(string userId)
        {
            _logger.LogInformation("Retrieving cart items for user {UserId}", userId);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetCartItems");
                
                string query = @"
                    SELECT oi.ID, oi.OrderID, oi.ProductID, oi.Quantity,
                           p.ID AS ProductID, p.Name, p.Price, p.ImageUrl
                    FROM cart oi  
                    JOIN products p ON oi.ProductID = p.ID
                    WHERE oi.UserID = @UserID;";

                var cartItems = await _db.QueryAsync<OrderItemsModel, ProductModel, OrderItemsModel>(
                    query,
                    (item, product) =>
                    {
                        item.ProductID = product.ID;
                        item.Product = product ?? new ProductModel();
                        return item;
                    },
                    new { UserID = userId },
                    splitOn: "ProductID"
                );

                var items = cartItems.AsList();
                _logger.LogInformation("Retrieved {Count} cart items for user {UserId}", items.Count, userId);
                _telemetryClient.TrackEvent("CartItemsRetrieved", new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "ItemCount", items.Count.ToString() },
                    { "CartValue", items.Sum(i => i.Product?.Price * i.Quantity ?? 0).ToString("F2") }
                });
                
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart items for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetCartItems" },
                    { "UserId", userId }
                });
                throw;
            }
        }

        // Removes all items from the user's shopping cart
        public async Task<bool> ClearCartAsync(string userId)
        {
            _logger.LogInformation("Clearing cart for user {UserId}", userId);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("ClearCart");
                
                var cartItems = await GetCartItemsAsync(userId);
                string query = "DELETE FROM cart WHERE UserID = @UserID;";
                int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId });
                
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Successfully cleared cart for user {UserId}", userId);
                    _telemetryClient.TrackEvent("CartCleared", new Dictionary<string, string>
                    {
                        { "UserId", userId },
                        { "ItemCount", cartItems.Count.ToString() },
                        { "CartValue", cartItems.Sum(i => i.Product?.Price * i.Quantity ?? 0).ToString("F2") }
                    });
                    return true;
                }
                
                _logger.LogWarning("No items found to clear in cart for user {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "ClearCart" },
                    { "UserId", userId }
                });
                throw;
            }
        }

        // Converts cart items into an order and clears the cart
        public async Task<bool> CheckoutAsync(string userId)
        {
            _logger.LogInformation("Processing checkout for user {UserId}", userId);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("Checkout");
                
                string orderId = Guid.NewGuid().ToString();
                var cartItems = await GetCartItemsAsync(userId);
                
                using var transaction = _db.BeginTransaction();
                try
                {
                    string createOrderQuery = @"
                        INSERT INTO orders (ID, UserID, CreatedAt) 
                        VALUES (@ID, @UserID, NOW());";

                    string moveCartToOrderQuery = @"
                        INSERT INTO order_items (OrderID, ProductID, Quantity)
                        SELECT @OrderID, ProductID, Quantity FROM cart WHERE UserID = @UserID;";

                    string clearCartQuery = "DELETE FROM cart WHERE UserID = @UserID;";

                    await _db.ExecuteAsync(createOrderQuery, new { ID = orderId, UserID = userId }, transaction);
                    await _db.ExecuteAsync(moveCartToOrderQuery, new { OrderID = orderId, UserID = userId }, transaction);
                    await _db.ExecuteAsync(clearCartQuery, new { UserID = userId }, transaction);

                    transaction.Commit();
                    
                    _logger.LogInformation("Checkout completed successfully for user {UserId}", userId);
                    _telemetryClient.TrackEvent("CheckoutCompleted", new Dictionary<string, string>
                    {
                        { "UserId", userId },
                        { "OrderId", orderId },
                        { "ItemCount", cartItems.Count.ToString() },
                        { "OrderValue", cartItems.Sum(i => i.Product?.Price * i.Quantity ?? 0).ToString("F2") }
                    });
                    
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Checkout transaction failed", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout for user {UserId}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "Checkout" },
                    { "UserId", userId }
                });
                return false;
            }
        }

        // Retrieves all orders for a specific user sorted by creation date
        public async Task<List<OrderModel>> GetOrdersByUserAsync(string userId)
        {
            string query = "SELECT * FROM orders WHERE UserID = @UserID ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query, new { UserID = userId })).AsList();
        }

        // Gets all items in a specific order with their product details
        public async Task<List<OrderItemsModel>> GetOrderItemsAsync(string orderId)
        {
            string query = @"
                SELECT oi.*, p.Name, p.Price
                FROM order_items oi
                JOIN products p ON oi.ProductID = p.ID
                WHERE oi.OrderID = @OrderID;";

            return (await _db.QueryAsync<OrderItemsModel>(query, new { OrderID = orderId })).AsList();
        }

        // Adds or updates an item in an existing order with the specified quantity
        public async Task<bool> AddOrderItemAsync(string orderItemId, string orderId, string productId, int quantity)
        {
            string query = @"
                INSERT INTO order_items (ID, OrderID, ProductID, Quantity)
                VALUES (@OrderItemID, @OrderID, @ProductID, @Quantity)
                ON DUPLICATE KEY UPDATE Quantity = Quantity + @Quantity;";

            int rowsAffected = await _db.ExecuteAsync(query, new
            {
                OrderItemID = orderItemId,
                OrderID = orderId,
                ProductID = productId,
                Quantity = quantity
            });

            return rowsAffected > 0;
        }
    }
}
