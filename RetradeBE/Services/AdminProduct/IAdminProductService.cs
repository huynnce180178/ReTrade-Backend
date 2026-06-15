using System.Threading.Tasks;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IAdminProductService
    {
        IQueryable<ProductListDto> Query();
        Task<PagedResultDto<ProductListDto>> GetProductsForApprovalAsync(ProductSearchQueryDto query);
        Task<ProductResponseDto?> GetProductByIdAsync(string productId);
        Task<bool> ApproveProductAsync(string productId, AdminProductApprovalDto dto);
    }
}
