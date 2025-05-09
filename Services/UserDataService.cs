﻿using Dapper;
using MySql.Data.MySqlClient;
using CardMaxxing.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    /*** 
    * @class UserDataService
    * @description Service for user account management, role management, and authentication
    * @param {IDbConnection} db - Database connection injected via dependency injection
    * @param {ILogger<UserDataService>} logger - Logger for structured application logging
    * @param {TelemetryClient} telemetryClient - Application Insights telemetry client for tracing
    */
    public class UserDataService : IUserDataService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<UserDataService> _logger;
        private readonly TelemetryClient _telemetryClient;
        public UserDataService(
            IDbConnection db,
            ILogger<UserDataService> logger,
            TelemetryClient telemetryClient)
        {
            _db = db;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        /***
        * @method CheckEmailDuplicateAsync
        * @description Checks if a user email already exists in the system to prevent duplicate accounts
        * @param {string} email - Email address to check for duplication
        * @returns {Task<bool>} - True if duplicate found, false otherwise
        */
        public async Task<bool> CheckEmailDuplicateAsync(string email)
        {
            _logger.LogInformation("Checking for duplicate email: {Email}", email);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CheckEmailDuplicate");
                
                string query = "SELECT COUNT(*) FROM users WHERE Email = @Email;";
                int count = await _db.ExecuteScalarAsync<int>(query, new { Email = email });
                
                _logger.LogInformation("Email duplicate check result: {IsDuplicate}", count > 0);
                _telemetryClient.TrackEvent("EmailDuplicateCheck", new Dictionary<string, string>
                {
                    { "Email", email },
                    { "IsDuplicate", (count > 0).ToString() }
                });
                
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email duplicate: {Email}", email);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "CheckEmailDuplicate" },
                    { "Email", email }
                });
                throw;
            }
        }

        /***
        * @method CreateUserAsync
        * @description Creates a new user with hashed password and stores it in the database
        * @param {UserModel} user - User object containing account details
        * @returns {Task<bool>} - True if creation is successful, false otherwise
        */
        public async Task<bool> CreateUserAsync(UserModel user)
        {
            _logger.LogInformation("Creating new user: {Username}", user.Username);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateUser");
                
                string query = @"INSERT INTO users (ID, Username, Password, Email, FirstName, LastName, Active, Role)
                               VALUES (@ID, @Username, @Password, @Email, @FirstName, @LastName, @Active, @Role);";
                
                user.ID = Guid.NewGuid().ToString();
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                int rowsAffected = await _db.ExecuteAsync(query, user);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("User created successfully: {Username}", user.Username);
                    _telemetryClient.TrackEvent("UserCreated", new Dictionary<string, string>
                    {
                        { "UserId", user.ID },
                        { "Username", user.Username },
                        { "Role", user.Role }
                    });
                    return true;
                }

                _logger.LogWarning("Failed to create user: {Username}", user.Username);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", user.Username);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "CreateUser" },
                    { "Username", user.Username }
                });
                throw;
            }
        }

        /***
        * @method DeleteUserAsync
        * @description Deletes a user account by ID from the database
        * @param {string} id - Unique user ID
        * @returns {Task<bool>} - True if deletion is successful, false otherwise
        */
        public async Task<bool> DeleteUserAsync(string id)
        {
            _logger.LogInformation("Deleting user with ID: {UserID}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("DeleteUser");
                
                string query = "DELETE FROM users WHERE ID = @ID;";
                int rowsAffected = await _db.ExecuteAsync(query, new { ID = id });

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("User deleted successfully: {UserID}", id);
                    _telemetryClient.TrackEvent("UserDeleted", new Dictionary<string, string>
                    {
                        { "UserId", id }
                    });
                    return true;
                }

                _logger.LogWarning("Failed to delete user: {UserID}", id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserID}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "DeleteUser" },
                    { "UserId", id }
                });
                throw;
            }
        }

        /***
        * @method EditUserAsync
        * @description Edits an existing user account's information, including password and role
        * @param {UserModel} user - Updated user object
        * @returns {Task<bool>} - True if update is successful, false otherwise
        */
        public async Task<bool> EditUserAsync(UserModel user)
        {
            _logger.LogInformation("Editing user: {Username}", user.Username);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("EditUser");
                
                string query = @"UPDATE users SET Username = @Username, Password = @Password, 
                               Email = @Email, FirstName = @FirstName, LastName = @LastName, Active = @Active, Role = @Role
                               WHERE ID = @ID;";
                
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                int rowsAffected = await _db.ExecuteAsync(query, user);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("User edited successfully: {Username}", user.Username);
                    _telemetryClient.TrackEvent("UserEdited", new Dictionary<string, string>
                    {
                        { "UserId", user.ID },
                        { "Username", user.Username },
                        { "Role", user.Role }
                    });
                    return true;
                }

                _logger.LogWarning("Failed to edit user: {Username}", user.Username);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing user: {Username}", user.Username);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "EditUser" },
                    { "Username", user.Username }
                });
                throw;
            }
        }

        /***
        * @method GetUserByIDAsync
        * @description Retrieves full user information by user ID
        * @param {string} id - Unique user ID
        * @returns {Task<UserModel>} - UserModel object if found, null otherwise
        */
        public async Task<UserModel> GetUserByIDAsync(string id)
        {
            _logger.LogInformation("Retrieving user by ID: {UserID}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetUserByID");
                
                string query = "SELECT * FROM users WHERE ID = @ID;";
                var user = await _db.QueryFirstOrDefaultAsync<UserModel>(query, new { ID = id });

                if (user != null)
                {
                    _logger.LogInformation("User retrieved successfully: {UserID}", id);
                    _telemetryClient.TrackEvent("UserRetrievedByID", new Dictionary<string, string>
                    {
                        { "UserId", id }
                    });
                }
                else
                {
                    _logger.LogWarning("User not found: {UserID}", id);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserID}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetUserByID" },
                    { "UserId", id }
                });
                throw;
            }
        }

        /***
        * @method GetUserByUsernameAsync
        * @description Retrieves full user information by username
        * @param {string} username - User's unique username
        * @returns {Task<UserModel>} - UserModel object if found, null otherwise
        */
        public async Task<UserModel> GetUserByUsernameAsync(string username)
        {
            _logger.LogInformation("Retrieving user by username: {Username}", username);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetUserByUsername");
                
                string query = "SELECT * FROM users WHERE Username = @Username;";
                var user = await _db.QueryFirstOrDefaultAsync<UserModel>(query, new { Username = username });

                if (user != null)
                {
                    _logger.LogInformation("User retrieved successfully: {Username}", username);
                    _telemetryClient.TrackEvent("UserRetrievedByUsername", new Dictionary<string, string>
                    {
                        { "Username", username }
                    });
                }
                else
                {
                    _logger.LogWarning("User not found: {Username}", username);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetUserByUsername" },
                    { "Username", username }
                });
                throw;
            }
        }

        /***
        * @method GetHashedPasswordAsync
        * @description Fetches the hashed password associated with a username
        * @param {string} username - Username to retrieve password for
        * @returns {Task<string>} - Hashed password string
        */
        public async Task<string> GetHashedPasswordAsync(string username)
        {
            _logger.LogInformation("Retrieving hashed password for username: {Username}", username);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetHashedPassword");
                
                string query = "SELECT Password FROM users WHERE Username = @Username;";
                var hashedPassword = await _db.ExecuteScalarAsync<string>(query, new { Username = username });

                if (!string.IsNullOrEmpty(hashedPassword))
                {
                    _logger.LogInformation("Hashed password retrieved successfully for username: {Username}", username);
                    _telemetryClient.TrackEvent("HashedPasswordRetrieved", new Dictionary<string, string>
                    {
                        { "Username", username }
                    });
                }
                else
                {
                    _logger.LogWarning("Hashed password not found for username: {Username}", username);
                }

                return hashedPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hashed password for username: {Username}", username);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetHashedPassword" },
                    { "Username", username }
                });
                throw;
            }
        }

        /***
        * @method GetUserRoleAsync
        * @description Fetches the user role (Admin or User) by username
        * @param {string} username - Username to retrieve role for
        * @returns {Task<string>} - Role string (e.g., Admin, User)
        */
        public async Task<string> GetUserRoleAsync(string username)
        {
            _logger.LogInformation("Retrieving role for username: {Username}", username);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetUserRole");
                
                string query = "SELECT Role FROM users WHERE Username = @Username;";
                var role = await _db.ExecuteScalarAsync<string>(query, new { Username = username });

                if (!string.IsNullOrEmpty(role))
                {
                    _logger.LogInformation("Role retrieved successfully for username: {Username}", username);
                    _telemetryClient.TrackEvent("UserRoleRetrieved", new Dictionary<string, string>
                    {
                        { "Username", username },
                        { "Role", role }
                    });
                }
                else
                {
                    _logger.LogWarning("Role not found for username: {Username}", username);
                }

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role for username: {Username}", username);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetUserRole" },
                    { "Username", username }
                });
                throw;
            }
        }

        /***
        * @method VerifyAccountAsync
        * @description Verifies if the input password matches the stored hashed password
        * @param {string} username - Username for login
        * @param {string} password - Plain-text password for comparison
        * @returns {Task<bool>} - True if password matches, false otherwise
        */
        public async Task<bool> VerifyAccountAsync(string username, string password)
        {
            _logger.LogInformation("Verifying account for username: {Username}", username);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("VerifyAccount");
                
                string storedHashedPassword = await GetHashedPasswordAsync(username);
                if (string.IsNullOrEmpty(storedHashedPassword))
                {
                    _logger.LogWarning("No hashed password found for username: {Username}", username);
                    return false;
                }

                bool isVerified = BCrypt.Net.BCrypt.Verify(password, storedHashedPassword);
                _logger.LogInformation("Account verification result for username: {Username} - {IsVerified}", username, isVerified);
                _telemetryClient.TrackEvent("AccountVerified", new Dictionary<string, string>
                {
                    { "Username", username },
                    { "IsVerified", isVerified.ToString() }
                });

                return isVerified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying account for username: {Username}", username);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "VerifyAccount" },
                    { "Username", username }
                });
                return false;
            }
        }
        /***
        * @method GetAllUsersAsync
        * @description Retrieves a list of all registered users
        * @returns {Task<List<UserModel>>} - List of user models
        */
        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            _logger.LogInformation("Retrieving all users");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetAllUsers");
                
                string query = "SELECT * FROM users;";
                var users = (await _db.QueryAsync<UserModel>(query)).AsList();

                _logger.LogInformation("All users retrieved successfully");
                _telemetryClient.TrackEvent("AllUsersRetrieved");

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetAllUsers" }
                });
                throw;
            }
        }

        /***
        * @method UpdateUserRoleAsync
        * @description Updates the role (Admin/User) of an existing user
        * @param {string} userId - Unique user ID
        * @param {string} newRole - New role to assign (Admin or User)
        * @returns {Task<bool>} - True if update is successful, false otherwise
        */
        public async Task<bool> UpdateUserRoleAsync(string userId, string newRole)
        {
            _logger.LogInformation("Updating role for user ID: {UserID} to {NewRole}", userId, newRole);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("UpdateUserRole");
                
                string query = "UPDATE users SET Role = @Role WHERE ID = @UserID;";
                int rowsAffected = await _db.ExecuteAsync(query, new { UserID = userId, Role = newRole });

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("User role updated successfully for user ID: {UserID}", userId);
                    _telemetryClient.TrackEvent("UserRoleUpdated", new Dictionary<string, string>
                    {
                        { "UserId", userId },
                        { "NewRole", newRole }
                    });
                    return true;
                }

                _logger.LogWarning("Failed to update role for user ID: {UserID}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user ID: {UserID}", userId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "UpdateUserRole" },
                    { "UserId", userId },
                    { "NewRole", newRole }
                });
                throw;
            }
        }
    }
}
