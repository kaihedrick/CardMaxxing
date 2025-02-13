namespace CardMaxxing.Models
{
    public class ProductModel
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
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
