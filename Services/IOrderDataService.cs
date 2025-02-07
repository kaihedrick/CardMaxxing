using CardMaxxing.Models;
using System.Collections.Generic;

namespace CardMaxxing.Services
{
    public interface IOrderDataService
    {
        public bool createOrder(OrderModel order);
        public bool deleteOrder(string id);
        public OrderModel getOrderByID(string id);
        public List<OrderModel> getOrdersByUserID(string userId);
    }
}
