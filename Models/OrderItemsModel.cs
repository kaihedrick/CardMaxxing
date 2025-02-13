namespace CardMaxxing.Models
{
    public class OrderItemsModel
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string OrderID { get; set; }
        public string ProductID { get; set; }
        public int Quantity { get; set; }

        public OrderItemsModel() { }

        public OrderItemsModel(string id, string orderId, string productId, int quantity)
        {
            ID = id;
            OrderID = orderId;
            ProductID = productId;
            Quantity = quantity;
        }
    }
}
