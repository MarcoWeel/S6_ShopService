namespace ShopService.Services.Interfaces;

using ShopService.Models;

public interface IProductService
{
    void SubscribeToGlobal();
    Task<List<Product>> GetProductsAsync();
    Task<Product> GetProductAsync(Guid id);
    Task<Product> UpdateProductAsync(Product updated);
    Task<Product> SaveProductAsync(Product product);
    Task DeleteProductAsync(Guid id);
}

