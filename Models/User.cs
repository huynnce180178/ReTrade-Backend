using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class User
{
    public string UserId { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<AuctionDeposit> AuctionDeposits { get; set; } = new List<AuctionDeposit>();

    public virtual ICollection<Auction> AuctionSellers { get; set; } = new List<Auction>();

    public virtual ICollection<Auction> AuctionWinners { get; set; } = new List<Auction>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<ChatRoom> ChatRoomBuyers { get; set; } = new List<ChatRoom>();

    public virtual ICollection<ChatRoom> ChatRoomSellers { get; set; } = new List<ChatRoom>();

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual ICollection<MyService> MyServices { get; set; } = new List<MyService>();

    public virtual ICollection<MyVoucher> MyVouchers { get; set; } = new List<MyVoucher>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();

    public virtual ICollection<Review> ReviewReviewers { get; set; } = new List<Review>();

    public virtual ICollection<Review> ReviewSellers { get; set; } = new List<Review>();

    public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();

    public virtual ICollection<UserFollow> UserFollowFollowedUsers { get; set; } = new List<UserFollow>();

    public virtual ICollection<UserFollow> UserFollowFollowers { get; set; } = new List<UserFollow>();

    public virtual ICollection<UserSearch> UserSearches { get; set; } = new List<UserSearch>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
