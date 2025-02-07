namespace CardMaxxing.Models
{
    public class ProductModel
    {
        // Properties
        public string ID { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }

        // Default Constructor
        public ProductModel()
        {
            this.ID = "";
            this.Name = string.Empty;
            this.Manufacturer = string.Empty;
            this.Description = string.Empty;
            this.Price = 0.0m;
            this.Quantity = 0;
            this.ImageUrl = string.Empty;
        }

        // Parameterized Constructor
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
