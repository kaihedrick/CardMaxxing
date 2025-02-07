namespace CardMaxxing.Models
{
    public class LoginModel
    {
        // Properties
        public string Username { get; set; }
        public string Password { get; set; }

        // Default Constructor
        public LoginModel()
        {
            this.Username = "";
            this.Password = "";
        }

        // Parameterized Constructor
        public LoginModel(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
