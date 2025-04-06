using Dapper;
using MySql.Data.MySqlClient;
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
    public class OrderDataService : IOrderDataService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<OrderDataService> _logger;
        private readonly TelemetryClient _telemetryClient;

        public OrderDataService(
            IDbConnection db,
            ILogger<OrderDataService> logger,
            TelemetryClient telemetryClient)
        {
            _db = db;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        // Creates a basic order record without items
        public async Task<bool> CreateOrderAsync(OrderModel order)
        {
            _logger.LogInformation("Creating new order for user {UserId}", order.UserID);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateOrder");
                
                string query = @"INSERT INTO orders (ID, UserID, CreatedAt)
                               VALUES (@ID, @UserID, @CreatedAt);";

                int rowsAffected = await _db.ExecuteAsync(query, order);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", 
                        order.ID, order.UserID);
                    _telemetryClient.TrackEvent("OrderCreated", new Dictionary<string, string>
                    {
                        { "OrderId", order.ID },
                        { "UserId", order.UserID }
                    });
                    return true;
                }

                _logger.LogWarning("Failed to create order for user {UserId}", order.UserID);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for user {UserId}", order.UserID);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "CreateOrder" },
                    { "UserId", order.UserID }
                });
                throw;
            }
        }

        // Creates an order with multiple items and handles inventory updates in a transaction
        public async Task<bool> CreateOrderWithItemsAsync(OrderModel order, List<OrderItemsModel> items)
        {
            _logger.LogInformation("Creating order {OrderId} with {ItemCount} items for user {UserId}", 
                order.ID, items.Count, order.UserID);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateOrderWithItems");
                using var connection = new MySqlConnection(_db.ConnectionString);
                
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();
                
                try
                {
                    string createOrderQuery = @"
                        INSERT INTO orders (ID, UserID, CreatedAt) 
                        VALUES (@ID, @UserID, NOW());";

                    int orderInserted = await connection.ExecuteAsync(createOrderQuery, order, transaction);

                    if (orderInserted == 0)
                    {
                        _logger.LogWarning("Failed to insert order header for {OrderId}", order.ID);
                        await transaction.RollbackAsync();
                        return false;
                    }

                    decimal orderTotal = 0;
                    foreach (var item in items)
                    {
                        string addItemQuery = @"
                            INSERT INTO order_items (ID, OrderID, ProductID, Quantity)
                            VALUES (@ID, @OrderID, @ProductID, @Quantity);";

                        int itemInserted = await connection.ExecuteAsync(addItemQuery, new
                        {
                            ID = Guid.NewGuid().ToString(),
                            OrderID = order.ID,
                            item.ProductID,
                            item.Quantity
                        }, transaction);

                        if (itemInserted == 0)
                        {
                            _logger.LogWarning("Failed to insert order item {ProductId} for order {OrderId}", 
                                item.ProductID, order.ID);
                            await transaction.RollbackAsync();
                            return false;
                        }

                        string updateStockQuery = @"
                            UPDATE products 
                            SET Quantity = Quantity - @Quantity
                            WHERE ID = @ProductID AND Quantity >= @Quantity;";

                        int stockUpdated = await connection.ExecuteAsync(updateStockQuery, new
                        {
                            item.ProductID,
                            item.Quantity
                        }, transaction);

                        if (stockUpdated == 0)
                        {
                            _logger.LogWarning("Insufficient stock for product {ProductId} in order {OrderId}", 
                                item.ProductID, order.ID);
                            await transaction.RollbackAsync();
                            return false;
                        }

                        orderTotal += item.Product?.Price * item.Quantity ?? 0;
                    }

                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Order {OrderId} created successfully with total ${OrderTotal}", 
                        order.ID, orderTotal);
                    _telemetryClient.TrackEvent("OrderWithItemsCreated", new Dictionary<string, string>
                    {
                        { "OrderId", order.ID },
                        { "UserId", order.UserID },
                        { "ItemCount", items.Count.ToString() },
                        { "OrderTotal", orderTotal.ToString("F2") }
                    });
                    
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Transaction failed for order {order.ID}", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order with items for user {UserId}", order.UserID);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "CreateOrderWithItems" },
                    { "OrderId", order.ID },
                    { "UserId", order.UserID }
                });
                return false;
            }
        }

        // Removes an order and its associated items from the database
        public async Task<bool> DeleteOrderAsync(string id)
        {
            _logger.LogInformation("Deleting order {OrderId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("DeleteOrder");
                
                string query = "DELETE FROM orders WHERE ID = @ID;";
                int rowsAffected = await _db.ExecuteAsync(query, new { ID = id });
                
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Order {OrderId} deleted successfully", id);
                    _telemetryClient.TrackEvent("OrderDeleted", new Dictionary<string, string>
                    {
                        { "OrderId", id }
                    });
                    return true;
                }
                
                _logger.LogWarning("Order {OrderId} not found for deletion", id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "DeleteOrder" },
                    { "OrderId", id }
                });
                throw;
            }
        }

        // Retrieves a single order by its unique identifier
        public async Task<OrderModel> GetOrderByIDAsync(string id)
        {
            string query = "SELECT * FROM orders WHERE ID = @ID;";
            return await _db.QueryFirstOrDefaultAsync<OrderModel>(query, new { ID = id });
        }

        // Gets all orders for a specific user sorted by creation date
        public async Task<List<OrderModel>> GetOrdersByUserIDAsync(string userId)
        {
            string query = "SELECT * FROM orders WHERE UserID = @UserID ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query, new { UserID = userId })).AsList();
        }

        // Retrieves all items in an order with their associated product details
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

        // Gets complete order details including items and totals for a user
        public async Task<List<(OrderModel, List<OrderItemsModel>, decimal)>> GetOrdersWithDetailsByUserIDAsync(string userId)
        {
            var orders = await GetOrdersByUserIDAsync(userId);
            var orderDetails = new List<(OrderModel, List<OrderItemsModel>, decimal)>();

            foreach (var order in orders)
            {
                var items = await GetOrderItemsByOrderIDAsync(order.ID);
                decimal total = await GetOrderTotalAsync(order.ID);
                orderDetails.Add((order, items, total));
            }

            return orderDetails;
        }

        // Calculates the total price of all items in an order
        public async Task<decimal> GetOrderTotalAsync(string orderId)
        {
            string query = @"
        SELECT COALESCE(SUM(p.Price * oi.Quantity), 0)
        FROM order_items oi
        JOIN products p ON oi.ProductID = p.ID
        WHERE oi.OrderID = @OrderID;";

            return await _db.ExecuteScalarAsync<decimal>(query, new { OrderID = orderId });
        }

        // Retrieves all orders in the system sorted by creation date
        public async Task<List<OrderModel>> GetAllOrdersAsync()
        {
            string query = "SELECT * FROM orders ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query)).AsList();
        }
    }
}
