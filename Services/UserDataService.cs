using Dapper;
using MySql.Data.MySqlClient;
using CardMaxxing.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    public class UserDataService : IUserDataService
    {
        private readonly IDbConnection _db;

        // Initialize with a database connection.
        public UserDataService(IDbConnection db)
        {
            _db = db;
        }

        // Check if the email already exists.
        public async Task<bool> CheckEmailDuplicateAsync(string email)
        {
            string query = "SELECT COUNT(*) FROM users WHERE Email = @Email;";
            int count = await _db.ExecuteScalarAsync<int>(query, new { Email = email });
            return count > 0;
        }

        // Create a new user and hash the password.
        public async Task<bool> CreateUserAsync(UserModel user)
        {
            string query = @"INSERT INTO users (ID, Username, Password, Email, FirstName, LastName, Active, Role)
                             VALUES (@ID, @Username, @Password, @Email, @FirstName, @LastName, @Active, @Role);";
            user.ID = Guid.NewGuid().ToString();
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            int rowsAffected = await _db.ExecuteAsync(query, user);
            return rowsAffected > 0;
        }

        // Delete a user by ID.
        public async Task<bool> DeleteUserAsync(string id)
        {
            string query = "DELETE FROM users WHERE ID = @ID;";
            int rowsAffected = await _db.ExecuteAsync(query, new { ID = id });
            return rowsAffected > 0;
        }

        // Update user details and hash the new password.
        public async Task<bool> EditUserAsync(UserModel user)
        {
            string query = @"UPDATE users SET Username = @Username, Password = @Password, 
                             Email = @Email, FirstName = @FirstName, LastName = @LastName, Active = @Active, Role = @Role
                             WHERE ID = @ID;";
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            int rowsAffected = await _db.ExecuteAsync(query, user);
            return rowsAffected > 0;
        }

        // Retrieve a user by ID.
        public async Task<UserModel> GetUserByIDAsync(string id)
        {
            string query = "SELECT * FROM users WHERE ID = @ID;";
            return await _db.QueryFirstOrDefaultAsync<UserModel>(query, new { ID = id });
        }

        // Retrieve a user by username.
        public async Task<UserModel> GetUserByUsernameAsync(string username)
        {
            string query = "SELECT * FROM users WHERE Username = @Username;";
            return await _db.QueryFirstOrDefaultAsync<UserModel>(query, new { Username = username });
        }

        // Get the hashed password for a username.
        public async Task<string> GetHashedPasswordAsync(string username)
        {
            string query = "SELECT Password FROM users WHERE Username = @Username;";
            return await _db.ExecuteScalarAsync<string>(query, new { Username = username });
        }

        // Get the role for a username.
        public async Task<string> GetUserRoleAsync(string username)
        {
            string query = "SELECT Role FROM users WHERE Username = @Username;";
            return await _db.ExecuteScalarAsync<string>(query, new { Username = username });
        }

        // Verify the user's account by comparing the provided password with the stored hash.
        public async Task<bool> VerifyAccountAsync(string username, string password)
        {
            try
            {
                string storedHashedPassword = await GetHashedPasswordAsync(username);
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

        // Retrieve all users from the database.
        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            string query = "SELECT * FROM users;";
            return (await _db.QueryAsync<UserModel>(query)).AsList();
        }

        // Update the role of a user.
        public async Task<bool> UpdateUserRoleAsync(string userId, string newRole)
        {
            string query = "UPDATE users SET Role = @Role WHERE ID = @UserID;";
            int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId, Role = newRole });
            return rowsAffected > 0;
        }
    }
}
