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
/*** 
 * @class OrderDataService
 * @description Service class responsible for managing order-related operations including creation, retrieval, and deletion.
 * @param {IDbConnection} db - Database connection for executing queries
 * @param {ILogger<OrderDataService>} logger - Logger instance for structured logging
 * @param {TelemetryClient} telemetryClient - Application Insights client for telemetry tracking
 */
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

/***
 * @method CreateOrderAsync
 * @description Creates a new order without order items
 * @param {OrderModel} order - Order object containing user ID and creation time
 * @returns {Task<bool>} - True if creation succeeds, false otherwise
 */
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

/***
 * @method CreateOrderWithItemsAsync
 * @description Creates an order and its associated items within a database transaction, ensuring stock is decremented
 * @param {OrderModel} order - Order header data
 * @param {List<OrderItemsModel>} items - List of order item entries
 * @returns {Task<bool>} - True if both order and items creation succeed, false otherwise
 */
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


/***
 * @method DeleteOrderAsync
 * @description Deletes an order from the database by its ID
 * @param {string} id - Unique identifier of the order
 * @returns {Task<bool>} - True if deletion is successful, false otherwise
 */

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


/***
 * @method GetOrderByIDAsync
 * @description Retrieves a specific order by its unique identifier
 * @param {string} id - Unique identifier of the order
 * @returns {Task<OrderModel>} - Order object if found, otherwise null
 */
        public async Task<OrderModel> GetOrderByIDAsync(string id)
        {
            string query = "SELECT * FROM orders WHERE ID = @ID;";
            return await _db.QueryFirstOrDefaultAsync<OrderModel>(query, new { ID = id });
        }

/***
 * @method GetOrderItemsByOrderIDAsync
 * @description Retrieves all items for a specific order, including associated product details
 * @param {string} orderId - Unique identifier of the order
 * @returns {Task<List<OrderItemsModel>>} - List of order items
 */

        public async Task<List<OrderModel>> GetOrdersByUserIDAsync(string userId)
        {
            string query = "SELECT * FROM orders WHERE UserID = @UserID ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query, new { UserID = userId })).AsList();
        }

/***
 * @method GetOrdersWithDetailsByUserIDAsync
 * @description Retrieves all orders with their items and total amounts for a specific user
 * @param {string} userId - Unique identifier of the user
 * @returns {Task<List<(OrderModel, List<OrderItemsModel>, decimal)>>} - List of tuples containing order, items, and totals
 */

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

/***
 * @method GetOrdersByUserIDAsync
 * @description Retrieves all orders associated with a specific user ID
 * @param {string} userId - Unique identifier of the user
 * @returns {Task<List<OrderModel>>} - List of user orders
 */
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

/***
 * @method GetOrderTotalAsync
 * @description Calculates the total price of all items for a given order
 * @param {string} orderId - Unique identifier of the order
 * @returns {Task<decimal>} - Total price of the order
 */
        public async Task<decimal> GetOrderTotalAsync(string orderId)
        {
            string query = @"
        SELECT COALESCE(SUM(p.Price * oi.Quantity), 0)
        FROM order_items oi
        JOIN products p ON oi.ProductID = p.ID
        WHERE oi.OrderID = @OrderID;";

            return await _db.ExecuteScalarAsync<decimal>(query, new { OrderID = orderId });
        }

/***
 * @method GetAllOrdersAsync
 * @description Retrieves all orders in the system sorted by creation date
 * @returns {Task<List<OrderModel>>} - List of all orders
 */
        public async Task<List<OrderModel>> GetAllOrdersAsync()
        {
            string query = "SELECT * FROM orders ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query)).AsList();
        }

/***
 * @method GetAllOrdersWithDetailsAsync
 * @description Retrieves all orders along with user names, items, and total prices
 * @returns {Task<List<(OrderModel, string, List<OrderItemsModel>, decimal)>>} - List of full order details
 */
        public async Task<List<(OrderModel, string, List<OrderItemsModel>, decimal)>> GetAllOrdersWithDetailsAsync()
        {
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetAllOrdersWithDetails");
                
                // Get all orders
                string orderQuery = "SELECT * FROM orders ORDER BY CreatedAt DESC;";
                var orders = (await _db.QueryAsync<OrderModel>(orderQuery)).AsList();
                
                // Get all order items with products in a single query
                string itemsQuery = @"
                    SELECT oi.ID, oi.OrderID, oi.ProductID, oi.Quantity,
                           p.ID AS ProductID, p.Name AS Name, p.Price, p.ImageUrl
                    FROM order_items oi  
                    JOIN products p ON oi.ProductID = p.ID;";
                
                var allItems = (await _db.QueryAsync<OrderItemsModel, ProductModel, OrderItemsModel>(
                    itemsQuery,
                    (item, product) =>
                    {
                        item.Product = product;
                        return item;
                    },
                    splitOn: "ProductID"
                )).AsList();
                
                // Group items by order ID
                var itemsByOrder = allItems.GroupBy(i => i.OrderID)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                // Get all users in a single query
                string userQuery = "SELECT * FROM users;";
                var users = (await _db.QueryAsync<UserModel>(userQuery))
                    .ToDictionary(u => u.ID, u => $"{u.FirstName} {u.LastName}");
                
                // Assemble the results
                var result = new List<(OrderModel, string, List<OrderItemsModel>, decimal)>();
                foreach (var order in orders)
                {
                    var orderItems = itemsByOrder.ContainsKey(order.ID) ? itemsByOrder[order.ID] : new List<OrderItemsModel>();
                    var userName = users.ContainsKey(order.UserID) ? users[order.UserID] : "Unknown User";
                    var totalPrice = orderItems.Sum(i => i.Product?.Price * i.Quantity ?? 0);
                    
                    result.Add((order, userName, orderItems, totalPrice));
                }
                
                _logger.LogInformation("Retrieved {OrderCount} orders with details", orders.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders with details");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetAllOrdersWithDetails" }
                });
                throw;
            }
        }
    }
}
