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

    public int? FlagCount { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Account> Account { get; set; } = new List<Account>();

    public virtual ICollection<Address> Address { get; set; } = new List<Address>();

    public virtual ICollection<AuctionDeposit> AuctionDeposit { get; set; } = new List<AuctionDeposit>();

    public virtual ICollection<AuctionDepositTransaction> AuctionDepositTransaction { get; set; } = new List<AuctionDepositTransaction>();

    public virtual ICollection<Auction> AuctionSeller { get; set; } = new List<Auction>();

    public virtual ICollection<Auction> AuctionWinner { get; set; } = new List<Auction>();

    public virtual ICollection<Bid> Bid { get; set; } = new List<Bid>();

    public virtual ICollection<Chat> Chat { get; set; } = new List<Chat>();

    public virtual ICollection<ChatRoom> ChatRoomBuyer { get; set; } = new List<ChatRoom>();

    public virtual ICollection<ChatRoom> ChatRoomSeller { get; set; } = new List<ChatRoom>();

    public virtual ICollection<MyService> MyService { get; set; } = new List<MyService>();

    public virtual ICollection<MyVoucher> MyVoucher { get; set; } = new List<MyVoucher>();

    public virtual ICollection<Notification> Notification { get; set; } = new List<Notification>();

    public virtual ICollection<Offer> Offer { get; set; } = new List<Offer>();

    public virtual ICollection<Order> OrderSeller { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderUser { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payment { get; set; } = new List<Payment>();

    public virtual ICollection<Product> Product { get; set; } = new List<Product>();

    public virtual ICollection<RefundRequest> RefundRequest { get; set; } = new List<RefundRequest>();

    public virtual ICollection<Review> ReviewReviewer { get; set; } = new List<Review>();

    public virtual ICollection<ReviewReport> ReviewReportReporter { get; set; } = new List<ReviewReport>();

    public virtual ICollection<ReviewReport> ReviewReportReviewedByNavigation { get; set; } = new List<ReviewReport>();

    public virtual ICollection<Review> ReviewSeller { get; set; } = new List<Review>();

    public virtual ICollection<UserFavorite> UserFavorite { get; set; } = new List<UserFavorite>();

    public virtual ICollection<UserFollow> UserFollowFollowedUser { get; set; } = new List<UserFollow>();

    public virtual ICollection<UserFollow> UserFollowFollower { get; set; } = new List<UserFollow>();

    public virtual ICollection<UserReport> UserReportReporter { get; set; } = new List<UserReport>();

    public virtual ICollection<UserReport> UserReportReviewedByNavigation { get; set; } = new List<UserReport>();

    public virtual ICollection<UserSearch> UserSearch { get; set; } = new List<UserSearch>();

    public virtual ICollection<Voucher> Voucher { get; set; } = new List<Voucher>();

    public virtual ICollection<Wishlist> Wishlist { get; set; } = new List<Wishlist>();
}
