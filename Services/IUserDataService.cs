using CardMaxxing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    public interface IUserDataService
    {
        Task<bool> CreateUserAsync(UserModel user);
        Task<bool> EditUserAsync(UserModel user);
        Task<bool> DeleteUserAsync(string id);
        Task<bool> VerifyAccountAsync(string username, string password);
        Task<UserModel> GetUserByUsernameAsync(string username);
        Task<UserModel> GetUserByIDAsync(string id);
        Task<string> GetHashedPasswordAsync(string username);
        Task<bool> CheckEmailDuplicateAsync(string email);
        Task<string> GetUserRoleAsync(string username); 
    }
}
