using Dapper;
using CardMaxxing.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    public class CartDataService : ICartDataService
    {
        private readonly IDbConnection _db;
        private readonly IProductDataService _productService;

        // Initialize service with database connection and product service.
        public CartDataService(IDbConnection db, IProductDataService productService)
        {
            _db = db;
            _productService = productService;
        }

        // Add a product to the user's cart or increment its quantity.
        public async Task<bool> AddToCartAsync(string userId, string productId)
        {
            string query = @"
                INSERT INTO cart (UserID, ProductID, Quantity) 
                VALUES (@UserID, @ProductID, 1) 
                ON DUPLICATE KEY UPDATE Quantity = Quantity + 1;";

            int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId, ProductID = productId });
            return rowsAffected > 0;
        }

        // Remove a product from the user's cart or decrement its quantity.
        public async Task<bool> RemoveFromCartAsync(string userId, string productId)
        {
            using (var transaction = _db.BeginTransaction())
            {
                try
                {
                    string decrementQuery = @"
                        UPDATE cart SET Quantity = Quantity - 1 
                        WHERE UserID = @UserID AND ProductID = @ProductID AND Quantity > 1;";

                    string deleteQuery = "DELETE FROM cart WHERE UserID = @UserID AND ProductID = @ProductID AND Quantity <= 1;";

                    await _db.ExecuteAsync(decrementQuery, new { UserID = userId, ProductID = productId }, transaction);
                    await _db.ExecuteAsync(deleteQuery, new { UserID = userId, ProductID = productId }, transaction);

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"[ERROR] RemoveFromCartAsync: {ex.Message}");
                    return false;
                }
            }
        }

        // Retrieve all items in the user's cart with their product details.
        public async Task<List<OrderItemsModel>> GetCartItemsAsync(string userId)
        {
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

            return cartItems.AsList();
        }

        // Clear all items from the user's cart.
        public async Task<bool> ClearCartAsync(string userId)
        {
            string query = "DELETE FROM cart WHERE UserID = @UserID;";
            int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId });
            return rowsAffected > 0;
        }

        // Create an order from the cart items and then clear the cart.
        public async Task<bool> CheckoutAsync(string userId)
        {
            string orderId = Guid.NewGuid().ToString();
            string createOrderQuery = @"
        INSERT INTO orders (ID, UserID, CreatedAt) 
        VALUES (@ID, @UserID, NOW());";

            string moveCartToOrderQuery = @"
        INSERT INTO order_items (OrderID, ProductID, Quantity)
        SELECT @OrderID, ProductID, Quantity FROM cart WHERE UserID = @UserID;";

            string clearCartQuery = "DELETE FROM cart WHERE UserID = @UserID;";

            using (var transaction = _db.BeginTransaction())
            {
                try
                {
                    await _db.ExecuteAsync(createOrderQuery, new { ID = orderId, UserID = userId }, transaction);
                    await _db.ExecuteAsync(moveCartToOrderQuery, new { OrderID = orderId, UserID = userId }, transaction);
                    await _db.ExecuteAsync(clearCartQuery, new { UserID = userId }, transaction);

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"[ERROR] CheckoutAsync: {ex.Message}");
                    return false;
                }
            }
        }

        // Get all orders made by the user.
        public async Task<List<OrderModel>> GetOrdersByUserAsync(string userId)
        {
            string query = "SELECT * FROM orders WHERE UserID = @UserID ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query, new { UserID = userId })).AsList();
        }

        // Retrieve all items for a specific order with product details.
        public async Task<List<OrderItemsModel>> GetOrderItemsAsync(string orderId)
        {
            string query = @"
                SELECT oi.*, p.Name, p.Price
                FROM order_items oi
                JOIN products p ON oi.ProductID = p.ID
                WHERE oi.OrderID = @OrderID;";

            return (await _db.QueryAsync<OrderItemsModel>(query, new { OrderID = orderId })).AsList();
        }

        // Add a new order item or update its quantity if it exists.
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
