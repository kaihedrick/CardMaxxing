namespace CardMaxxing.Models
{
    public class OrderItemsModel
    {
        // Properties
        public string ID { get; set; }  // Primary Key
        public string OrderID { get; set; }  // Foreign Key to Orders
        public string ProductID { get; set; }  // Foreign Key to Products
        public int Quantity { get; set; }  // Number of this item in the order

        // Default constructor
        public OrderItemsModel()
        {
        }

        // Parameterized constructor
        public OrderItemsModel(string id, string orderId, string productId, int quantity)
        {
            ID = id;
            OrderID = orderId;
            ProductID = productId;
            Quantity = quantity;
        }
    }
}
