using RetradeBE.Models;

namespace RetradeBE.Repositories
{
    public interface IAuctionRepository
    {
        IQueryable<Auction> Query();
        IQueryable<Product> QueryEligibleProducts();
        Task<Auction?> GetByIdAsync(string auctionId);
        Task AddAsync(Auction auction);
        Task UpdateAsync(Auction auction);
        Task<bool> HasOpenAuctionForProductAsync(string productId);
    }
}
