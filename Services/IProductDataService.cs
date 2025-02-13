using CardMaxxing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    public interface IProductDataService
    {
        Task<bool> CreateProductAsync(ProductModel product);
        Task<bool> EditProductAsync(ProductModel product);  
        Task<bool> DeleteProductAsync(string id);
        Task<ProductModel> GetProductByIDAsync(string id);
        Task<List<ProductModel>> GetAllProductsAsync();
        Task<bool> CheckProductDuplicateAsync(string name);
        Task<List<ProductModel>> SearchProductsAsync(string searchTerm);  
    }
}
