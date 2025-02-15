using Dapper;
using MySql.Data.MySqlClient;
using CardMaxxing.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    public class OrderDataService : IOrderDataService
    {
        private readonly IDbConnection _db;

        public OrderDataService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<bool> CreateOrderAsync(OrderModel order)
        {
            string query = @"INSERT INTO orders (ID, UserID, CreatedAt)
                             VALUES (@ID, @UserID, @CreatedAt);";

            int rowsAffected = await _db.ExecuteAsync(query, new
            {
                order.ID,
                order.UserID,
                order.CreatedAt
            });

            return rowsAffected > 0;
        }
        public async Task<bool> CreateOrderWithItemsAsync(OrderModel order, List<OrderItemsModel> items)
        {
            using (var connection = new MySqlConnection(_db.ConnectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync()) // ✅ Ensure atomic operations
                {
                    try
                    {
                        // ✅ Step 1: Insert the Order
                        string createOrderQuery = @"
                    INSERT INTO orders (ID, UserID, CreatedAt) 
                    VALUES (@ID, @UserID, NOW());";

                        int orderInserted = await connection.ExecuteAsync(createOrderQuery, new
                        {
                            order.ID,
                            order.UserID
                        }, transaction);

                        if (orderInserted == 0)
                        {
                            await transaction.RollbackAsync();
                            return false;
                        }

                        // ✅ Step 2: Insert Order Items
                        string addItemQuery = @"
                    INSERT INTO order_items (ID, OrderID, ProductID, Quantity)
                    VALUES (@ID, @OrderID, @ProductID, @Quantity);";

                        foreach (var item in items)
                        {
                            int itemInserted = await connection.ExecuteAsync(addItemQuery, new
                            {
                                ID = Guid.NewGuid().ToString(),
                                OrderID = order.ID,
                                ProductID = item.ProductID,
                                Quantity = item.Quantity
                            }, transaction);

                            if (itemInserted == 0)
                            {
                                await transaction.RollbackAsync();
                                return false;
                            }

                            // ✅ Step 3: Update Product Stock
                            string updateStockQuery = @"
                        UPDATE products 
                        SET Quantity = Quantity - @Quantity
                        WHERE ID = @ProductID AND Quantity >= @Quantity;";

                            int stockUpdated = await connection.ExecuteAsync(updateStockQuery, new
                            {
                                ProductID = item.ProductID,
                                Quantity = item.Quantity
                            }, transaction);

                            if (stockUpdated == 0) // 🚨 Stock issue
                            {
                                await transaction.RollbackAsync();
                                return false;
                            }
                        }

                        // ✅ Step 4: Commit Transaction
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"[ERROR] CreateOrderWithItemsAsync: {ex.Message}");
                        return false;
                    }
                }
            }
        }




        public async Task<bool> DeleteOrderAsync(string id)
        {
            string query = "DELETE FROM orders WHERE ID = @ID;";
            int rowsAffected = await _db.ExecuteAsync(query, new { ID = id });
            return rowsAffected > 0;
        }

        public async Task<OrderModel> GetOrderByIDAsync(string id)
        {
            string query = "SELECT * FROM orders WHERE ID = @ID;";
            return await _db.QueryFirstOrDefaultAsync<OrderModel>(query, new { ID = id });
        }

        public async Task<List<OrderModel>> GetOrdersByUserIDAsync(string userId)
        {
            string query = "SELECT * FROM orders WHERE UserID = @UserID ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query, new { UserID = userId })).AsList();
        }

        public async Task<List<OrderItemsModel>> GetOrderItemsByOrderIDAsync(string orderId)
        {
            string query = @"
        SELECT oi.ID, oi.OrderID, oi.ProductID, oi.Quantity,
               p.ID AS ProductID, p.Name AS Name, p.Price, p.ImageUrl
        FROM order_items oi  
        JOIN products p ON oi.ProductID = p.ID
        WHERE oi.OrderID = @OrderID;";

            var orderItems = await _db.QueryAsync<OrderItemsModel, ProductModel, OrderItemsModel>(
                query,
                (item, product) =>
                {
                    item.Product = product ?? new ProductModel
                    {
                        ID = item.ProductID,
                        Name = "Unknown Product",
                        Price = 0m,
                        ImageUrl = "default.jpg"
                    };
                    return item;
                },
                new { OrderID = orderId },
                splitOn: "ProductID"
            );

            return orderItems.AsList();
        }

        public async Task<List<(OrderModel, List<OrderItemsModel>, decimal)>> GetOrdersWithDetailsByUserIDAsync(string userId)
        {
            var orders = await GetOrdersByUserIDAsync(userId);
            var orderDetails = new List<(OrderModel, List<OrderItemsModel>, decimal)>();

            foreach (var order in orders)
            {
                var items = await GetOrderItemsByOrderIDAsync(order.ID);
                decimal total = await GetOrderTotalAsync(order.ID);
                orderDetails.Add((order, items, total)); // ✅ Ensure the correct tuple structure
            }

            return orderDetails;
        }

        public async Task<decimal> GetOrderTotalAsync(string orderId)
        {
            string query = @"
        SELECT COALESCE(SUM(p.Price * oi.Quantity), 0)
        FROM order_items oi
        JOIN products p ON oi.ProductID = p.ID
        WHERE oi.OrderID = @OrderID;";

            return await _db.ExecuteScalarAsync<decimal>(query, new { OrderID = orderId });
        }


        public async Task<List<OrderModel>> GetAllOrdersAsync()
        {
            string query = "SELECT * FROM orders ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query)).AsList();
        }
    }
}
