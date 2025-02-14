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
        public CartDataService(IDbConnection db, IProductDataService productService)
        {
            _db = db;
            _productService = productService;
        }

        public async Task<bool> AddToCartAsync(string userId, string productId)
        {
            string query = @"
                INSERT INTO cart (UserID, ProductID, Quantity) 
                VALUES (@UserID, @ProductID, 1) 
                ON DUPLICATE KEY UPDATE Quantity = Quantity + 1;";

            int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId, ProductID = productId });
            return rowsAffected > 0;
        }

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

        public async Task<List<OrderItemsModel>> GetCartItemsAsync(string userId)
        {
            string query = @"
        SELECT oi.ID, oi.ProductID, oi.Quantity,
               p.ID AS ProductID, p.Name, p.Price, p.ImageUrl
        FROM order_items oi
        JOIN products p ON oi.ProductID = p.ID
        WHERE oi.OrderID IS NULL AND oi.UserID = @UserID;";

            var cartItems = await _db.QueryAsync<OrderItemsModel, ProductModel, OrderItemsModel>(
                query,
                (item, product) =>
                {
                    item.ProductID = product.ID; // Keep Product ID
                    item.Product = product ?? new ProductModel(); // Attach Product details
                    return item;
                },
                new { UserID = userId },
                splitOn: "ProductID"
            );

            return cartItems.AsList();
        }
 




        public async Task<bool> ClearCartAsync(string userId)
        {
            string query = "DELETE FROM cart WHERE UserID = @UserID;";
            int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId });
            return rowsAffected > 0;
        }

        public async Task<bool> CheckoutAsync(string userId)
        {
            using (var transaction = _db.BeginTransaction())
            {
                try
                {
                    string orderId = Guid.NewGuid().ToString();
                    string createOrderQuery = @"
                        INSERT INTO orders (ID, UserID, CreatedAt) 
                        VALUES (@ID, @UserID, NOW());";

                    await _db.ExecuteAsync(createOrderQuery, new { ID = orderId, UserID = userId }, transaction);

                    string addOrderItemsQuery = @"
                        INSERT INTO order_items (OrderID, ProductID, Quantity) 
                        SELECT @OrderID, ProductID, Quantity FROM cart WHERE UserID = @UserID;";

                    await _db.ExecuteAsync(addOrderItemsQuery, new { OrderID = orderId, UserID = userId }, transaction);

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

        public async Task<List<OrderModel>> GetOrdersByUserAsync(string userId)
        {
            string query = "SELECT * FROM orders WHERE UserID = @UserID ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query, new { UserID = userId })).AsList();
        }

        public async Task<List<OrderItemsModel>> GetOrderItemsAsync(string orderId)
        {
            string query = @"
                SELECT oi.*, p.Name, p.Price
                FROM order_items oi
                JOIN products p ON oi.ProductID = p.ID
                WHERE oi.OrderID = @OrderID;";

            return (await _db.QueryAsync<OrderItemsModel>(query, new { OrderID = orderId })).AsList();
        }

        public async Task<decimal> GetOrderTotalAsync(string orderId)
        {
            string query = @"
                SELECT SUM(p.Price * oi.Quantity) 
                FROM order_items oi
                JOIN products p ON oi.ProductID = p.ID
                WHERE oi.OrderID = @OrderID;";

            return await _db.ExecuteScalarAsync<decimal>(query, new { OrderID = orderId });
        }
    }
}
