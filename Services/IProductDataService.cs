using CardMaxxing.Models;

namespace CardMaxxing.Services
{
    public interface IProductDataService
    {
        public bool createProduct(ProductModel product);
        public bool editProduct(ProductModel product);
        public bool deleteProduct(string id);
        public ProductModel getProductByID(string id);
        public List<ProductModel> getAllProducts();
        bool checkProductDuplicate(string name);
        List<ProductModel> searchProducts(string searchTerm, string category);

    }
}
