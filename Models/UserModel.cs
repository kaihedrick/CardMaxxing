using System.ComponentModel.DataAnnotations;

namespace CardMaxxing.Models
{
    public class UserModel
    {
        //properties
        public string ID { get; set; } // Auto-generated unique ID

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        public bool? Active { get; set; }



        //default constructor
        public UserModel()
        {
            this.ID = "";
            this.Username = string.Empty;
            this.Password = string.Empty;
            this.Email = string.Empty;
            this.FirstName = string.Empty;
            this.LastName = string.Empty;
            this.Active = false;
        }
        //parameterized constructor
        public UserModel(string id, string username, string password, string email, string firstName, string lastName, bool active)
        {
            ID = id;
            Username = username;
            Password = password;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            Active = active;
        }
    }
}
