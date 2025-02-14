using CardMaxxing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    public interface IOrderDataService
    {
        Task<bool> CreateOrderAsync(OrderModel order);
        Task<bool> DeleteOrderAsync(string id);
        Task<OrderModel> GetOrderByIDAsync(string id);
        Task<List<OrderModel>> GetOrdersByUserIDAsync(string userId);
        Task<List<OrderItemsModel>> GetOrderItemsByOrderIDAsync(string orderId);
        Task<List<OrderModel>> GetAllOrdersAsync();
    }
}
