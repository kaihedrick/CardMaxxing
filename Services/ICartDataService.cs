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
    }
}
