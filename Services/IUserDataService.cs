using CardMaxxing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
/*** 
 * @interface IUserDataService
 * @description Interface defining user-related data operations for CRUD and authentication functionalities
 */
    public interface IUserDataService
    {

 /***
 * @method CreateUserAsync
 * @description Creates a new user record in the database
 * @param {UserModel} user - User model containing user data
 * @returns {Task<bool>} - True if user creation succeeds, otherwise false
 */
        Task<bool> CreateUserAsync(UserModel user);

/***
 * @method EditUserAsync
 * @description Updates an existing user's profile information
 * @param {UserModel} user - Updated user model
 * @returns {Task<bool>} - True if user update succeeds, otherwise false
 */
        Task<bool> EditUserAsync(UserModel user);

/***
 * @method DeleteUserAsync
 * @description Deletes a user from the database by their unique identifier
 * @param {string} id - Unique identifier of the user
 * @returns {Task<bool>} - True if user deletion succeeds, otherwise false
 */
        Task<bool> DeleteUserAsync(string id);

 /***
 * @method VerifyAccountAsync
 * @description Verifies a user's credentials by matching username and password
 * @param {string} username - Username of the user
 * @param {string} password - Plain text password to verify
 * @returns {Task<bool>} - True if credentials are valid, otherwise false
 */
        Task<bool> VerifyAccountAsync(string username, string password);

 /***
 * @method GetUserByUsernameAsync
 * @description Retrieves a user profile by their username
 * @param {string} username - Username to search
 * @returns {Task<UserModel>} - User model if found, otherwise null
 */
        Task<UserModel> GetUserByUsernameAsync(string username);

 /***
 * @method GetUserByIDAsync
 * @description Retrieves a user profile by their unique identifier
 * @param {string} id - Unique identifier of the user
 * @returns {Task<UserModel>} - User model if found, otherwise null
 */

        Task<UserModel> GetUserByIDAsync(string id);
/***
 * @method GetHashedPasswordAsync
 * @description Retrieves the stored hashed password for a given username
 * @param {string} username - Username to retrieve hashed password
 * @returns {Task<string>} - Hashed password string
 */
        Task<string> GetHashedPasswordAsync(string username);
/***
 * @method CheckEmailDuplicateAsync
 * @description Checks if an email address already exists in the system
 * @param {string} email - Email address to verify
 * @returns {Task<bool>} - True if email is already used, otherwise false
 */
        Task<bool> CheckEmailDuplicateAsync(string email);
/***
 * @method GetUserRoleAsync
 * @description Retrieves the role assigned to a user
 * @param {string} username - Username to lookup role
 * @returns {Task<string>} - Role of the user
 */
        Task<string> GetUserRoleAsync(string username);
/***
 * @method UpdateUserRoleAsync
 * @description Updates the role of an existing user
 * @param {string} userId - Unique identifier of the user
 * @param {string} newRole - New role to assign
 * @returns {Task<bool>} - True if role update succeeds, otherwise false
 */
        Task<bool> UpdateUserRoleAsync(string userId, string newRole);
/***
 * @method GetAllUsersAsync
 * @description Retrieves all registered users from the database
 * @returns {Task<List<UserModel>>} - List of all users
 */
        Task<List<UserModel>> GetAllUsersAsync();
    }
}
