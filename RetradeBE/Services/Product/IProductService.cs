using System.Threading.Tasks;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IProductService
    {
        Task<ProductResponseDto> CreateProductAsync(string accountId, ProductCreateDto dto);
        Task<ProductResponseDto> UpdateProductAsync(string productId, string accountId, ProductUpdateDto dto);
        Task<ProductResponseDto?> GetProductByIdAsync(string productId);
        Task<PagedResultDto<ProductListDto>> GetProductsAsync(ProductSearchQueryDto query);
        Task DeleteProductAsync(string productId, string accountId);
    }
}
