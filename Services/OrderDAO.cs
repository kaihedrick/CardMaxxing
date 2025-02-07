using CardMaxxing.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace CardMaxxing.Services
{
    public class OrderDAO : IOrderDataService
    {
        private string connectionString = "Server=127.0.0.1;User=root;Password=root;Database=cardmaxxing;";

        // Create a new order
        public bool createOrder(OrderModel order)
        {
            string sqlStatement = "INSERT INTO cardmaxxing.orders (UserID, CreatedAt) VALUES (@UserID, @CreatedAt);";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sqlStatement, connection);

                command.Parameters.AddWithValue("@UserID", order.UserID);
                command.Parameters.AddWithValue("@CreatedAt", order.CreatedAt);

                try
                {
                    connection.Open();
                    return command.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating order: {ex.Message}");
                    return false;
                }
            }
        }

        // Delete an order by ID
        public bool deleteOrder(string id)
        {
            string sqlStatement = "DELETE FROM cardmaxxing.orders WHERE ID = @ID;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sqlStatement, connection);
                command.Parameters.AddWithValue("@ID", id);

                try
                {
                    connection.Open();
                    return command.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting order: {ex.Message}");
                    return false;
                }
            }
        }

        // Retrieve an order by its ID
        public OrderModel getOrderByID(string id) // FIX: Change int to string
        {
            OrderModel order = null; // FIX: Use OrdersModel

            string sqlStatement = "SELECT * FROM cardmaxxing.orders WHERE ID = @ID;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sqlStatement, connection);
                command.Parameters.AddWithValue("@ID", id); // FIX: Use string

                try
                {
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        order = new OrderModel // FIX: Use OrdersModel
                        {
                            ID = reader["ID"].ToString(), // FIX: Convert to string
                            UserID = reader["UserID"].ToString(), // FIX: Convert to string
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving order: {ex.Message}");
                }
            }

            return order;
        }


        // Retrieve all orders for a specific user
        public List<OrderModel> getOrdersByUserID(string userId) // FIX: Change int to string
        {
            List<OrderModel> orders = new List<OrderModel>();

            string sqlStatement = "SELECT * FROM cardmaxxing.orders WHERE UserID = @UserID ORDER BY CreatedAt DESC;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sqlStatement, connection);
                command.Parameters.AddWithValue("@UserID", userId); // FIX: Use string

                try
                {
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        OrderModel order = new OrderModel // FIX: Use OrdersModel instead of OrderModel
                        {
                            ID = reader["ID"].ToString(), // FIX: Convert to string
                            UserID = reader["UserID"].ToString(), // FIX: Convert to string
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        };
                        orders.Add(order);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving orders: {ex.Message}");
                }
            }

            return orders;
        }
    }
}
