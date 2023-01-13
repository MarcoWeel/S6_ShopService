using ShopService.Models;

namespace ShopService.Services.Interfaces
{
    public interface IOrderService
    {
        void SubscribeToGlobal();
        Task<List<Order>> GetOrdersAsync();
        Task<Order> GetOrderAsync(Guid id);
        Task<Order> UpdateOrderAsync(Order updated);
        Task<Order> SaveOrderAsync(Order material);
        Task DeleteOrderAsync(Guid id);
    }
}
