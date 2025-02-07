namespace CardMaxxing.Models
{
    public class OrderModel
    {
        // Properties
        public string ID { get; set; }
        public string UserID { get; set; }
        public DateTime CreatedAt { get; set; }

        // Default constructor
        public OrderModel()
        {
        }

        // Parameterized constructor
        public OrderModel(string id, string userId, DateTime createdAt)
        {
            ID = id;
            UserID = userId;
            CreatedAt = createdAt;
        }
    }
}
