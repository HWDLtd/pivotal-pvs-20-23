using TabletController.Core.DTOs;

namespace TabletController.Core.Interfaces
{
    public interface IProductDataService
    {
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<Product?> GetProductByIdAsync(int productId);
        Task<Product?> GetProductByBarcodeAsync(string barcode);
    }
}

