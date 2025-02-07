using CardMaxxing.Models;
using MySql.Data.MySqlClient;

namespace CardMaxxing.Services
{
    public class ProductDAO : IProductDataService
    {
        string connectionString = "Server=127.0.0.1;User=root;Password=root;Database=cardmaxxing;";

        public bool checkProductDuplicate(string name)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM cardmaxxing.products WHERE Name = @Name", connection);
                    cmd.Parameters.AddWithValue("@Name", name);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    return reader.HasRows;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return false;
            }
        }

        public bool createProduct(ProductModel product)
        {
            string sqlStatement = "INSERT INTO cardmaxxing.products (ID, Name, Manufacturer, Description, Price, Quantity, ImageUrl) VALUES (@ID, @Name, @Manufacturer, @Description, @Price, @Quantity, @ImageUrl)";

            string newProductId = Guid.NewGuid().ToString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sqlStatement, connection);

                command.Parameters.AddWithValue("@ID", newProductId);
                command.Parameters.AddWithValue("@Name", product.Name);
                command.Parameters.AddWithValue("@Manufacturer", product.Manufacturer);
                command.Parameters.AddWithValue("@Description", product.Description);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Quantity", product.Quantity);
                command.Parameters.AddWithValue("@ImageUrl", product.ImageUrl);

                try
                {
                    connection.Open();
                    return command.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        public bool deleteProduct(string id)
        {
            string sqlStatement = "DELETE FROM cardmaxxing.products WHERE ID = @ID";

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
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        public bool editProduct(ProductModel product)
        {
            string sqlStatement = "UPDATE cardmaxxing.products SET Name = @Name, Manufacturer = @Manufacturer, Description = @Description, Price = @Price, Quantity = @Quantity, ImageUrl = @ImageUrl WHERE ID = @ID;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sqlStatement, connection);

                command.Parameters.AddWithValue("@ID", product.ID);
                command.Parameters.AddWithValue("@Name", product.Name);
                command.Parameters.AddWithValue("@Manufacturer", product.Manufacturer);
                command.Parameters.AddWithValue("@Description", product.Description);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Quantity", product.Quantity);
                command.Parameters.AddWithValue("@ImageUrl", product.ImageUrl);

                try
                {
                    connection.Open();
                    return command.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        public ProductModel getProductByID(string id)
        {
            ProductModel product = new ProductModel();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM cardmaxxing.products WHERE ID = @ID", connection);
                    cmd.Parameters.AddWithValue("@ID", id);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        product.ID = reader["ID"].ToString() ?? "";
                        product.Name = reader["Name"].ToString() ?? "";
                        product.Manufacturer = reader["Manufacturer"].ToString() ?? "";
                        product.Description = reader["Description"].ToString() ?? "";
                        product.Price = Convert.ToDecimal(reader["Price"]);
                        product.Quantity = Convert.ToInt32(reader["Quantity"]);
                        product.ImageUrl = reader["ImageUrl"].ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return product;
        }

        public List<ProductModel> getAllProducts()
        {
            List<ProductModel> products = new List<ProductModel>();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM cardmaxxing.products", connection);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        products.Add(new ProductModel
                        {
                            ID = reader["ID"].ToString() ?? "",
                            Name = reader["Name"].ToString() ?? "",
                            Manufacturer = reader["Manufacturer"].ToString() ?? "",
                            Description = reader["Description"].ToString() ?? "",
                            Price = Convert.ToDecimal(reader["Price"]),
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            ImageUrl = reader["ImageUrl"].ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return products;
        }
        public List<ProductModel> searchProducts(string searchTerm, string category)
        {
            string sql = "SELECT * FROM products WHERE Name LIKE @SearchTerm OR Manufacturer LIKE @Category";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                command.Parameters.AddWithValue("@Category", "%" + category + "%");

                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                List<ProductModel> products = new List<ProductModel>();
                while (reader.Read())
                {
                    products.Add(new ProductModel
                    {
                        ID = reader["ID"].ToString(),
                        Name = reader["Name"].ToString(),
                        Manufacturer = reader["Manufacturer"].ToString(),
                        Description = reader["Description"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        ImageUrl = reader["ImageUrl"].ToString()
                    });
                }
                return products;
            }
        }

    }
}
