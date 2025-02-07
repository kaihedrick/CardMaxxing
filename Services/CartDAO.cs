using CardMaxxing.Services;
using MySql.Data.MySqlClient;
using System;

public class CartDAO : ICartDataService
{
    private string connectionString = "Server=127.0.0.1;User=root;Password=root;Database=cardmaxxing;";

    public bool addToCart(string userId, string productId)
    {
        string sql = "INSERT INTO cart (UserID, ProductID, Quantity) VALUES (@UserID, @ProductID, 1) ON DUPLICATE KEY UPDATE Quantity = Quantity + 1;";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            MySqlCommand command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserID", userId);
            command.Parameters.AddWithValue("@ProductID", productId);

            try
            {
                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to cart: {ex.Message}");
                return false;
            }
        }
    }
}
