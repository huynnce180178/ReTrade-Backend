using RetradeBE.Models.DTOs;

namespace RetradeBE.Services.Offer
{
    public interface IOfferService
    {
        Task<OfferDto> MakeOfferAsync(string accountId, MakeOfferRequestDto request);
        Task<List<OfferDto>> GetMyOffersAsync(string accountId, string? productId = null);
        Task<List<OfferDto>> GetOffersForProductAsync(string sellerId, string productId);
        Task<OfferDto> AcceptOfferAsync(string sellerId, string offerId);
        Task<OfferDto> RejectOfferAsync(string sellerId, string offerId);
        Task<OfferDto> CancelOfferAsync(string buyerUserId, string offerId);
        Task<string> CheckoutFromOfferAsync(OfferCheckoutRequestDto request, string accountId);
        Task<List<OfferDto>> GetOffersBySellerAsync(string sellerId);
        Task<OfferDto> CounterOfferAsync(string sellerId, CounterOfferDto request);
    }
}
