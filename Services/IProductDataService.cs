using CardMaxxing.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    /*** 
 * @interface IProductDataService
 * @description Interface defining product-related data operations for managing products in the database
 */
    public interface IProductDataService
    {
/***
 * @method CreateProductAsync
 * @description Creates a new product entry in the database
 * @param {ProductModel} product - Product model containing product information
 * @returns {Task<bool>} - True if product creation succeeds, otherwise false
 */

        Task<bool> CreateProductAsync(ProductModel product);

/***
 * @method EditProductAsync
 * @description Updates details of an existing product
 * @param {ProductModel} product - Updated product model
 * @returns {Task<bool>} - True if product update succeeds, otherwise false
 */
        Task<bool> EditProductAsync(ProductModel product);  
        /***
 * @method DeleteProductAsync
 * @description Deletes a product from the database by its unique identifier
 * @param {string} id - Unique identifier of the product
 * @returns {Task<bool>} - True if product deletion succeeds, otherwise false
 */

        Task<bool> DeleteProductAsync(string id);
        /***
 * @method GetProductByIDAsync
 * @description Retrieves a product by its unique identifier
 * @param {string} id - Unique identifier of the product
 * @returns {Task<ProductModel>} - Product model if found, otherwise null
 */
        Task<ProductModel> GetProductByIDAsync(string id);

/***
 * @method GetAllProductsAsync
 * @description Retrieves all products from the database
 * @returns {Task<List<ProductModel>>} - List of all products
 */
        Task<List<ProductModel>> GetAllProductsAsync();
        /***
 * @method CheckProductDuplicateAsync
 * @description Checks if a product with the same name already exists
 * @param {string} name - Name of the product to check for duplicates
 * @returns {Task<bool>} - True if duplicate exists, otherwise false
 */

        Task<bool> CheckProductDuplicateAsync(string name);
        /***
 * @method SearchProductsAsync
 * @description Searches for products based on a search term matching name or manufacturer
 * @param {string} searchTerm - Partial search term for product lookup
 * @returns {Task<List<ProductModel>>} - List of products matching the search criteria
 */

        Task<List<ProductModel>> SearchProductsAsync(string searchTerm);
        /***
 * @method DecreaseStockAsync
 * @description Decreases the available quantity of a product based on a purchase
 * @param {string} productId - Unique identifier of the product
 * @param {int} quantity - Quantity to subtract from the current stock
 * @returns {Task<bool>} - True if stock decrease succeeds, otherwise false
 */
        Task<bool> DecreaseStockAsync(string productId, int quantity);
    }
}
