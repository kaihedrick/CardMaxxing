using System;
using System.ComponentModel.DataAnnotations;

namespace CardMaxxing.Models
{
    public class ProductModel
    {
        [Key]
        public string ID { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Manufacturer is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Manufacturer must be between 2 and 100 characters.")]
        public string Manufacturer { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 255 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 100000, ErrorMessage = "Price must be a positive value.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Image URL is required.")]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        public string ImageUrl { get; set; }

        public ProductModel() { }

        public ProductModel(string id, string name, string manufacturer, string description, decimal price, int quantity, string imageUrl)
        {
            ID = id;
            Name = name;
            Manufacturer = manufacturer;
            Description = description;
            Price = price;
            Quantity = quantity;
            ImageUrl = imageUrl;
        }
    }
}
