using System.Collections.Generic;
using System.Threading.Tasks;
using CardMaxxing.Models;

namespace CardMaxxing.Services
{
    public interface ICartDataService
    {
        Task<bool> AddToCartAsync(string userId, string productId);
        Task<bool> RemoveFromCartAsync(string userId, string productId);
        Task<List<OrderItemsModel>> GetCartItemsAsync(string userId);
        Task<bool> ClearCartAsync(string userId);
        Task<bool> CheckoutAsync(string userId);
        Task<List<OrderModel>> GetOrdersByUserAsync(string userId);
        Task<List<OrderItemsModel>> GetOrderItemsAsync(string orderId);
        Task<bool> AddOrderItemAsync(string orderItemId, string orderId, string productId, int quantity);
    }
}
