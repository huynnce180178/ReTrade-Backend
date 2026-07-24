using RetradeBE.Models.DTOs;

namespace RetradeBE.Services.Offer
{
    public interface IOfferService
    {
        Task<OfferDto> MakeOfferAsync(string accountId, MakeOfferRequestDto request);
        Task<List<OfferDto>> GetMyOffersAsync(string accountId, string? productId = null);
        Task<List<OfferDto>> GetOffersForProductAsync(string sellerId, string productId);
        Task<OfferDto> RespondToOfferAsync(string sellerId, string offerId, bool accept);
        Task<OfferDto> CancelOfferAsync(string buyerUserId, string offerId);
        Task<string> CheckoutFromOfferAsync(OfferCheckoutRequestDto request, string accountId);
        Task<List<OfferDto>> GetOffersBySellerAsync(string sellerId);
        Task<OfferDto> CounterOfferAsync(string sellerId, CounterOfferDto request);
    }
}
