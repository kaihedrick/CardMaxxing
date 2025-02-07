

namespace CardMaxxing.Services
{
    public interface ICartDataService
    {
        bool addToCart(string userId, string productId);
    }
}
