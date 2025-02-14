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
            string query = "SELECT * FROM order_items WHERE OrderID = @OrderID;";
            return (await _db.QueryAsync<OrderItemsModel>(query, new { OrderID = orderId })).AsList();
        }

        public async Task<List<OrderModel>> GetAllOrdersAsync()
        {
            string query = "SELECT * FROM orders ORDER BY CreatedAt DESC;";
            return (await _db.QueryAsync<OrderModel>(query)).AsList();
        }
    }
}
