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

        public CartDataService(IDbConnection db)
        {
            _db = db;
        }

        // Add item to cart (Increase quantity if exists)
        public async Task<bool> AddToCartAsync(string userId, string productId)
        {
            string query = @"
                INSERT INTO cart (UserID, ProductID, Quantity) 
                VALUES (@UserID, @ProductID, 1) 
                ON DUPLICATE KEY UPDATE Quantity = Quantity + 1;";

            int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId, ProductID = productId });
            return rowsAffected > 0;
        }

        // Remove item from cart (Decrease quantity, delete if zero)
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

        // Get all items in the cart
        public async Task<List<OrderItemsModel>> GetCartItemsAsync(string userId)
        {
            string query = "SELECT ProductID, Quantity FROM cart WHERE UserID = @UserID;";
            return (await _db.QueryAsync<OrderItemsModel>(query, new { UserID = userId })).AsList();
        }

        // Clear cart
        public async Task<bool> ClearCartAsync(string userId)
        {
            string query = "DELETE FROM cart WHERE UserID = @UserID;";
            int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId });
            return rowsAffected > 0;
        }

        // Checkout: Convert cart items into an order
        public async Task<bool> CheckoutAsync(string userId)
        {
            using (var transaction = _db.BeginTransaction())
            {
                try
                {
                    // Create a new order
                    string orderId = Guid.NewGuid().ToString();
                    string createOrderQuery = @"
                        INSERT INTO orders (ID, UserID, CreatedAt, Status) 
                        VALUES (@ID, @UserID, NOW(), 'Pending');";

                    await _db.ExecuteAsync(createOrderQuery, new { ID = orderId, UserID = userId }, transaction);

                    // Insert cart items into order_items
                    string addOrderItemsQuery = @"
                        INSERT INTO order_items (OrderID, ProductID, Quantity) 
                        SELECT @OrderID, ProductID, Quantity FROM cart WHERE UserID = @UserID;";

                    await _db.ExecuteAsync(addOrderItemsQuery, new { OrderID = orderId, UserID = userId }, transaction);

                    // Clear cart after checkout
                    string clearCartQuery = "DELETE FROM cart WHERE UserID = @UserID;";
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
    }
}
