using CardMaxxing.Models;
using System.Collections.Generic;

namespace CardMaxxing.Services
{
    public interface IOrderDataService
    {
        public bool createOrder(OrderModel order);
        public bool deleteOrder(int id);
        public OrderModel getOrderByID(int id);
        public List<OrderModel> getOrdersByUserID(string userId);
    }
}
