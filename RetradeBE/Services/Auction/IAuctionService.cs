using RetradeBE.Models.DTOs;

namespace RetradeBE.Services
{
    public interface IAuctionService
    {
        Task<PagedResultDto<AuctionListDto>> GetAuctionsAsync(AuctionQueryDto query);
        Task<PagedResultDto<AuctionListDto>> GetMyAuctionsAsync(string accountId, AuctionQueryDto query);
        Task<AuctionDetailDto?> GetAuctionByIdAsync(string auctionId);
        Task<PagedResultDto<ProductListDto>> GetEligibleProductsAsync(string accountId, AuctionQueryDto query);
        Task<AuctionDetailDto> CreateAuctionAsync(string accountId, AuctionCreateDto dto);
        Task<AuctionDetailDto> UpdateAuctionAsync(string accountId, string auctionId, AuctionUpdateDto dto);
        Task<AuctionDepositDto?> GetMyDepositAsync(string accountId, string auctionId);
        Task<CreateVnPayPaymentResponseDto> CreateDepositPaymentUrlAsync(string accountId, string auctionId, AuctionDepositPaymentRequestDto dto, string ipAddress);
        Task<AuctionBidResultDto> PlaceBidAsync(string accountId, string auctionId, AuctionBidCreateDto dto);
        Task<int> ProcessDueAuctionsAsync(CancellationToken cancellationToken = default);
        Task<int> NotifyUpcomingAuctionsAsync(CancellationToken cancellationToken = default);
        Task<List<UserBidHistoryDto>> GetUserBidHistoryAsync(string accountId);
    }
}
