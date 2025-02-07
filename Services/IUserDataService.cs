using CardMaxxing.Models;

namespace CardMaxxing.Services
{
    public interface IUserDataService
    {
        public bool createUser(UserModel user);
        public bool editUser(UserModel user);
        public bool deleteUser(string id);
        public bool verifyAccount(string uname, string password);
        public UserModel getUserByUsername(string username);
        public UserModel getUserByID(string id);
        string getHashedPassword(string username);
        bool checkEmailDuplicate(string email);
    }
}
