using CardMaxxing.Models;
using MySql.Data.MySqlClient;

namespace CardMaxxing.Services
{
    public class UserDAO : IUserDataService
    {
        string connectionString = "Server=127.0.0.1;User=root;Password=root;Database=cardmaxxing;";

        public bool checkEmailDuplicate(string email)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM cardmaxxing.users WHERE Email = @Email", connection);
                    cmd.Parameters.AddWithValue("@Email", email);
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

        public bool createUser(UserModel user)
        {
            string sqlStatement = "INSERT INTO cardmaxxing.users (ID, Username, Password, Email, FirstName, LastName, Active) VALUES (@ID, @Username, @Password, @Email, @FirstName, @LastName, @Active)";

            string newUserId = Guid.NewGuid().ToString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sqlStatement, connection);

                command.Parameters.AddWithValue("@ID", newUserId);
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);
                command.Parameters.AddWithValue("@Active", user.Active);

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

        public bool deleteUser(string id)
        {
            string sqlStatement = "DELETE FROM cardmaxxing.users WHERE ID = @ID";

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

        public bool editUser(UserModel user)
        {
            string sqlStatement = "UPDATE cardmaxxing.users SET Username = @Username, Password = @Password, Email = @Email, FirstName = @FirstName, LastName = @LastName, Active = @Active WHERE ID = @Id;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(sqlStatement, connection);

                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);
                command.Parameters.AddWithValue("@Active", user.Active);
                command.Parameters.AddWithValue("@Id", user.ID);

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

        public string getHashedPassword(string username)
        {
            UserModel user = getUserByUsername(username);
            return user.Password;
        }

        public UserModel getUserByID(string id)
        {
            UserModel user = new UserModel();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM cardmaxxing.users WHERE ID = @ID", connection);
                    cmd.Parameters.AddWithValue("@ID", id);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        user.ID = reader["ID"].ToString() ?? "";
                        user.Username = reader["Username"].ToString() ?? "";
                        user.Password = reader["Password"].ToString() ?? "";
                        user.Email = reader["Email"].ToString() ?? "";
                        user.FirstName = reader["FirstName"].ToString() ?? "";
                        user.LastName = reader["LastName"].ToString() ?? "";
                        user.Active = Convert.ToBoolean(reader["Active"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return user;
        }

        public UserModel getUserByUsername(string username)
        {
            UserModel user = new UserModel();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM cardmaxxing.users WHERE Username = @Username", connection);
                    cmd.Parameters.AddWithValue("@Username", username);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        user.ID = reader["ID"].ToString() ?? "";
                        user.Username = reader["Username"].ToString() ?? "";
                        user.Password = reader["Password"].ToString() ?? "";
                        user.Email = reader["Email"].ToString() ?? "";
                        user.FirstName = reader["FirstName"].ToString() ?? "";
                        user.LastName = reader["LastName"].ToString() ?? "";
                        user.Active = Convert.ToBoolean(reader["Active"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return user;
        }

        public bool verifyAccount(string username, string password)
        {
            try
            {
                string storedHashedPassword = getHashedPassword(username);

                if (string.IsNullOrEmpty(storedHashedPassword))
                {
                    Console.WriteLine($"No hashed password found for username: {username}");
                    return false;
                }

                return BCrypt.Net.BCrypt.Verify(password, storedHashedPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying account: {ex.Message}");
                return false;
            }
        }
    }
}
