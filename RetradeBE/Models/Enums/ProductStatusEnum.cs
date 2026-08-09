namespace RetradeBE.Models.Enums
{
    public enum ProductStatusEnum
    {
        // Sale listing statuses
        Pending,
        Accepted,
        SaleRejected,

        // Auction listing statuses
        Waiting,
        Ready,
        AuctionRejected,

        // Additional statuses
        Sold,
        Inactive,
        Removed
    }
}
