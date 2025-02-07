using System.ComponentModel.DataAnnotations;

namespace CardMaxxing.Models
{
    public class LoginModel
    {
        // Properties
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
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