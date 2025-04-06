using Dapper;
using MySql.Data.MySqlClient;
using CardMaxxing.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CardMaxxing.Services
{
    public class ProductDataService : IProductDataService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<ProductDataService> _logger;
        private readonly TelemetryClient _telemetryClient;

        public ProductDataService(
            IDbConnection db,
            ILogger<ProductDataService> logger,
            TelemetryClient telemetryClient)
        {
            _db = db;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        // Verifies if a product with the same name already exists in the database
        public async Task<bool> CheckProductDuplicateAsync(string name)
        {
            _logger.LogInformation("Checking for duplicate product: {ProductName}", name);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CheckProductDuplicate");
                
                string query = "SELECT COUNT(*) FROM products WHERE Name = @Name;";
                int count = await _db.ExecuteScalarAsync<int>(query, new { Name = name });
                
                _logger.LogInformation("Duplicate check result for {ProductName}: {IsDuplicate}", name, count > 0);
                _telemetryClient.TrackEvent("ProductDuplicateCheck", new Dictionary<string, string>
                {
                    { "ProductName", name },
                    { "IsDuplicate", (count > 0).ToString() }
                });
                
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for duplicate product: {ProductName}", name);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "CheckProductDuplicate" },
                    { "ProductName", name }
                });
                throw;
            }
        }

        // Creates a new product in the database with a generated unique identifier
        public async Task<bool> CreateProductAsync(ProductModel product)
        {
            _logger.LogInformation("Creating new product: {ProductName}", product.Name);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("CreateProduct");
                
                string query = @"INSERT INTO products (ID, Name, Manufacturer, Description, Price, Quantity, ImageUrl)
                               VALUES (@ID, @Name, @Manufacturer, @Description, @Price, @Quantity, @ImageUrl);";

                product.ID = Guid.NewGuid().ToString();
                int rowsAffected = await _db.ExecuteAsync(query, product);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Product created successfully: {ProductId} - {ProductName}", 
                        product.ID, product.Name);
                    _telemetryClient.TrackEvent("ProductCreated", new Dictionary<string, string>
                    {
                        { "ProductId", product.ID },
                        { "ProductName", product.Name },
                        { "Price", product.Price.ToString("F2") }
                    });
                    return true;
                }

                _logger.LogWarning("Failed to create product: {ProductName}", product.Name);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", product.Name);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "CreateProduct" },
                    { "ProductName", product.Name }
                });
                throw;
            }
        }

        // Removes a product from the database using its unique identifier
        public async Task<bool> DeleteProductAsync(string id)
        {
            _logger.LogInformation("Deleting product: {ProductId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("DeleteProduct");
                
                string query = "DELETE FROM products WHERE ID = @ID;";
                int rowsAffected = await _db.ExecuteAsync(query, new { ID = id });

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Product deleted successfully: {ProductId}", id);
                    _telemetryClient.TrackEvent("ProductDeleted", new Dictionary<string, string>
                    {
                        { "ProductId", id }
                    });
                    return true;
                }

                _logger.LogWarning("Product not found for deletion: {ProductId}", id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "DeleteProduct" },
                    { "ProductId", id }
                });
                throw;
            }
        }

        // Retrieves a single product from the database by its ID
        public async Task<ProductModel> GetProductByIDAsync(string id)
        {
            _logger.LogInformation("Retrieving product: {ProductId}", id);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetProductById");
                
                string query = "SELECT * FROM products WHERE ID = @ID;";
                var product = await _db.QueryFirstOrDefaultAsync<ProductModel>(query, new { ID = id });

                if (product != null)
                {
                    _logger.LogInformation("Product retrieved successfully: {ProductId} - {ProductName}", 
                        id, product.Name);
                    _telemetryClient.TrackEvent("ProductRetrieved", new Dictionary<string, string>
                    {
                        { "ProductId", id },
                        { "ProductName", product.Name }
                    });
                }
                else
                {
                    _logger.LogWarning("Product not found: {ProductId}", id);
                }

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product: {ProductId}", id);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetProductById" },
                    { "ProductId", id }
                });
                throw;
            }
        }

        // Retrieves all products from the database
        public async Task<List<ProductModel>> GetAllProductsAsync()
        {
            _logger.LogInformation("Retrieving all products");
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("GetAllProducts");
                
                string query = "SELECT * FROM products;";
                var products = (await _db.QueryAsync<ProductModel>(query)).AsList();

                _logger.LogInformation("Retrieved {Count} products", products.Count);
                _telemetryClient.TrackEvent("AllProductsRetrieved", new Dictionary<string, string>
                {
                    { "ProductCount", products.Count.ToString() }
                });

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "GetAllProducts" }
                });
                throw;
            }
        }

        // Updates an existing product's information in the database
        public async Task<bool> EditProductAsync(ProductModel product)
        {
            _logger.LogInformation("Updating product: {ProductId} - {ProductName}", product.ID, product.Name);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("EditProduct");
                
                string query = @"
                    UPDATE products 
                    SET Name = @Name, 
                        Manufacturer = @Manufacturer, 
                        Description = @Description, 
                        Price = @Price, 
                        Quantity = @Quantity, 
                        ImageUrl = @ImageUrl
                    WHERE ID = @ID;";

                int rowsAffected = await _db.ExecuteAsync(query, product);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Product updated successfully: {ProductId} - {ProductName}", 
                        product.ID, product.Name);
                    _telemetryClient.TrackEvent("ProductUpdated", new Dictionary<string, string>
                    {
                        { "ProductId", product.ID },
                        { "ProductName", product.Name },
                        { "NewPrice", product.Price.ToString("F2") }
                    });
                    return true;
                }

                _logger.LogWarning("Product not found for update: {ProductId}", product.ID);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", product.ID);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "EditProduct" },
                    { "ProductId", product.ID }
                });
                throw;
            }
        }

        // Searches for products by name or manufacturer using partial matching
        public async Task<List<ProductModel>> SearchProductsAsync(string searchTerm)
        {
            _logger.LogInformation("Searching products with term: {SearchTerm}", searchTerm);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("SearchProducts");
                
                string query = @"SELECT * FROM products 
                               WHERE Name LIKE @SearchTerm OR Manufacturer LIKE @SearchTerm;";
                var products = (await _db.QueryAsync<ProductModel>(query, 
                    new { SearchTerm = "%" + searchTerm + "%" })).AsList();

                _logger.LogInformation("Search for '{SearchTerm}' returned {Count} results", 
                    searchTerm, products.Count);
                _telemetryClient.TrackEvent("ProductSearch", new Dictionary<string, string>
                {
                    { "SearchTerm", searchTerm },
                    { "ResultCount", products.Count.ToString() }
                });

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with term: {SearchTerm}", searchTerm);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "SearchProducts" },
                    { "SearchTerm", searchTerm }
                });
                throw;
            }
        }

        // Reduces the available quantity of a product in inventory
        public async Task<bool> DecreaseStockAsync(string productId, int quantity)
        {
            _logger.LogInformation("Decreasing stock for product {ProductId} by {Quantity}", productId, quantity);
            try
            {
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>("DecreaseStock");
                
                string query = @"
                    UPDATE products 
                    SET Quantity = Quantity - @Quantity 
                    WHERE ID = @ProductID AND Quantity >= @Quantity;";

                int rowsAffected = await _db.ExecuteAsync(query, 
                    new { ProductID = productId, Quantity = quantity });

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Stock decreased successfully for product {ProductId} by {Quantity}", 
                        productId, quantity);
                    _telemetryClient.TrackEvent("StockDecreased", new Dictionary<string, string>
                    {
                        { "ProductId", productId },
                        { "QuantityReduced", quantity.ToString() }
                    });
                    return true;
                }

                _logger.LogWarning("Insufficient stock for product {ProductId}, quantity requested: {Quantity}", 
                    productId, quantity);
                _telemetryClient.TrackEvent("InsufficientStock", new Dictionary<string, string>
                {
                    { "ProductId", productId },
                    { "QuantityRequested", quantity.ToString() }
                });
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decreasing stock for product {ProductId}", productId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "Operation", "DecreaseStock" },
                    { "ProductId", productId },
                    { "QuantityRequested", quantity.ToString() }
                });
                throw;
            }
        }
    }
}
