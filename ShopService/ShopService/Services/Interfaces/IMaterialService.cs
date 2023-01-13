using ShopService.Models;

namespace ShopService.Services.Interfaces
{
    public interface IMaterialService
    {
        void SubscribeToGlobal();
        Task<List<Material>> GetMaterialsAsync();
        Task<Material> GetMaterialAsync(Guid id);
        Task<Material> UpdateMaterialAsync(Material updated);
        Task<Material> SaveMaterialAsync(Material material);
        Task DeleteMaterialAsync(Guid id);
    }
}
