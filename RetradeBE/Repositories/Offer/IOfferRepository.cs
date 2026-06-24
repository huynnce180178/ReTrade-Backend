using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IOfferRepository
    {
        Task<List<Offer>> GetAllAsync();
        Task<Offer?> GetByIdAsync(string offerId);
        Task AddAsync(Offer offer);
        Task UpdateAsync(Offer offer);
        Task DeleteAsync(Offer offer);
        Task<List<Offer>> GetOffersBySellerAsync(string sellerId);
    }
}
