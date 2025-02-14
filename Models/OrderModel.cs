using System;

namespace CardMaxxing.Models
{
    public class OrderModel
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string UserID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public OrderModel() { }

        public OrderModel(string id, string userId, DateTime createdAt)
        {
            ID = id;
            UserID = userId;
            CreatedAt = createdAt;
        }
    }
}
