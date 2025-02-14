using System;
using System.ComponentModel.DataAnnotations;

namespace CardMaxxing.Models
{
    public class UserModel
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();

        [Required, StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; }

        [Required, StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(20, MinimumLength = 3)]
        public string Username { get; set; }

        [Required, DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; }
        public bool Active { get; set; } = false;

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }

        // Constructor to enforce default values, don't want a user being able to set their role as admin
        public UserModel()
        {
            Active = false;
            Role = "User";
        }
    }
}
