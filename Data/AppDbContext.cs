using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Models;

namespace RetradeBE.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AccountRole> AccountRoles { get; set; }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Models.Attribute> Attributes { get; set; }

    public virtual DbSet<Auction> Auctions { get; set; }

    public virtual DbSet<AuctionDeposit> AuctionDeposits { get; set; }

    public virtual DbSet<Banner> Banners { get; set; }

    public virtual DbSet<Bid> Bids { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CategoryImage> CategoryImages { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<ChatRoom> ChatRooms { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<MyService> MyServices { get; set; }

    public virtual DbSet<MyVoucher> MyVouchers { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Offer> Offers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductAttribute> ProductAttributes { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<RefundRequest> RefundRequests { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ServiceSubscription> ServiceSubscriptions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserFavorite> UserFavorites { get; set; }

    public virtual DbSet<UserFollow> UserFollows { get; set; }

    public virtual DbSet<UserSearch> UserSearches { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    public virtual DbSet<WishlistItem> WishlistItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=RetradeDB;Username=postgres;Password=123456");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("Account_pkey");

            entity.ToTable("Account");

            entity.Property(e => e.AccountId)
                .HasMaxLength(100)
                .HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.LastLoginAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Provider)
                .HasMaxLength(30)
                .HasColumnName("provider");
            entity.Property(e => e.ProviderUserId)
                .HasMaxLength(255)
                .HasColumnName("provider_user_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.User).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_account_user");
        });

        modelBuilder.Entity<AccountRole>(entity =>
        {
            entity.HasKey(e => new { e.AccountId, e.RoleId }).HasName("Account_Role_pkey");

            entity.ToTable("Account_Role");

            entity.Property(e => e.AccountId)
                .HasMaxLength(100)
                .HasColumnName("account_id");
            entity.Property(e => e.RoleId)
                .HasMaxLength(100)
                .HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountRoles)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_acc_role_account");

            entity.HasOne(d => d.Role).WithMany(p => p.AccountRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_acc_role_role");
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("Address_pkey");

            entity.ToTable("Address");

            entity.Property(e => e.AddressId)
                .HasMaxLength(100)
                .HasColumnName("address_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DistrictId).HasColumnName("district_id");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.ProvinceId).HasColumnName("province_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Street)
                .HasMaxLength(255)
                .HasColumnName("street");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");
            entity.Property(e => e.WardCode)
                .HasMaxLength(50)
                .HasColumnName("ward_code");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_address_user");
        });

        modelBuilder.Entity<Models.Attribute>(entity =>
        {
            entity.HasKey(e => e.AttributeId).HasName("Attribute_pkey");

            entity.ToTable("Attribute");

            entity.Property(e => e.AttributeId)
                .HasMaxLength(100)
                .HasColumnName("attribute_id");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(100)
                .HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DataType)
                .HasMaxLength(50)
                .HasColumnName("data_type");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsRequired).HasColumnName("is_required");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.Attributes)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_attribute_category");
        });

        modelBuilder.Entity<Auction>(entity =>
        {
            entity.HasKey(e => e.AuctionId).HasName("Auction_pkey");

            entity.ToTable("Auction");

            entity.Property(e => e.AuctionId)
                .HasMaxLength(100)
                .HasColumnName("auction_id");
            entity.Property(e => e.BuyNowPrice)
                .HasPrecision(18, 2)
                .HasColumnName("buy_now_price");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentPrice)
                .HasPrecision(18, 2)
                .HasColumnName("current_price");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.MinIncrement)
                .HasPrecision(18, 2)
                .HasColumnName("min_increment");
            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.ReservePrice)
                .HasPrecision(18, 2)
                .HasColumnName("reserve_price");
            entity.Property(e => e.SellerId)
                .HasMaxLength(100)
                .HasColumnName("seller_id");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.StartingPrice)
                .HasPrecision(18, 2)
                .HasColumnName("starting_price");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.WinnerId)
                .HasMaxLength(100)
                .HasColumnName("winner_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Auctions)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_auction_product");

            entity.HasOne(d => d.Seller).WithMany(p => p.AuctionSellers)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_auction_seller");

            entity.HasOne(d => d.Winner).WithMany(p => p.AuctionWinners)
                .HasForeignKey(d => d.WinnerId)
                .HasConstraintName("fk_auction_winner");
        });

        modelBuilder.Entity<AuctionDeposit>(entity =>
        {
            entity.HasKey(e => e.AuctionDepositId).HasName("Auction_Deposit_pkey");

            entity.ToTable("Auction_Deposit");

            entity.Property(e => e.AuctionDepositId)
                .HasMaxLength(100)
                .HasColumnName("auction_deposit_id");
            entity.Property(e => e.AuctionId)
                .HasMaxLength(100)
                .HasColumnName("auction_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DepositAmount)
                .HasPrecision(18, 2)
                .HasColumnName("deposit_amount");
            entity.Property(e => e.PolicyAccepted).HasColumnName("policy_accepted");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Auction).WithMany(p => p.AuctionDeposits)
                .HasForeignKey(d => d.AuctionId)
                .HasConstraintName("fk_deposit_auction");

            entity.HasOne(d => d.User).WithMany(p => p.AuctionDeposits)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_deposit_user");
        });

        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("Banner_pkey");

            entity.ToTable("Banner");

            entity.Property(e => e.BannerId)
                .HasMaxLength(100)
                .HasColumnName("banner_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.RedirectUrl).HasColumnName("redirect_url");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.BidId).HasName("Bid_pkey");

            entity.ToTable("Bid");

            entity.Property(e => e.BidId)
                .HasMaxLength(100)
                .HasColumnName("bid_id");
            entity.Property(e => e.AuctionId)
                .HasMaxLength(100)
                .HasColumnName("auction_id");
            entity.Property(e => e.BidAmount)
                .HasPrecision(18, 2)
                .HasColumnName("bid_amount");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Auction).WithMany(p => p.Bids)
                .HasForeignKey(d => d.AuctionId)
                .HasConstraintName("fk_bid_auction");

            entity.HasOne(d => d.User).WithMany(p => p.Bids)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_bid_user");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("Category_pkey");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId)
                .HasMaxLength(100)
                .HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<CategoryImage>(entity =>
        {
            entity.HasKey(e => new { e.CategoryId, e.ImageId }).HasName("Category_Image_pkey");

            entity.ToTable("Category_Image");

            entity.Property(e => e.CategoryId)
                .HasMaxLength(100)
                .HasColumnName("category_id");
            entity.Property(e => e.ImageId)
                .HasMaxLength(100)
                .HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Category).WithMany(p => p.CategoryImages)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_catimg_category");

            entity.HasOne(d => d.Image).WithMany(p => p.CategoryImages)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_catimg_image");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("Chat_pkey");

            entity.ToTable("Chat");

            entity.Property(e => e.ChatId)
                .HasMaxLength(100)
                .HasColumnName("chat_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.MessageType)
                .HasMaxLength(30)
                .HasColumnName("message_type");
            entity.Property(e => e.ReadAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("read_at");
            entity.Property(e => e.RoomId)
                .HasMaxLength(100)
                .HasColumnName("room_id");
            entity.Property(e => e.SenderId)
                .HasMaxLength(100)
                .HasColumnName("sender_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Room).WithMany(p => p.Chats)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("fk_chat_room");

            entity.HasOne(d => d.Sender).WithMany(p => p.Chats)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("fk_chat_sender");
        });

        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("Chat_Room_pkey");

            entity.ToTable("Chat_Room");

            entity.Property(e => e.RoomId)
                .HasMaxLength(100)
                .HasColumnName("room_id");
            entity.Property(e => e.BuyerId)
                .HasMaxLength(100)
                .HasColumnName("buyer_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.SellerId)
                .HasMaxLength(100)
                .HasColumnName("seller_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Buyer).WithMany(p => p.ChatRoomBuyers)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("fk_chatroom_buyer");

            entity.HasOne(d => d.Product).WithMany(p => p.ChatRooms)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_chatroom_product");

            entity.HasOne(d => d.Seller).WithMany(p => p.ChatRoomSellers)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_chatroom_seller");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("Image_pkey");

            entity.ToTable("Image");

            entity.Property(e => e.ImageId)
                .HasMaxLength(100)
                .HasColumnName("image_id");
            entity.Property(e => e.AltText)
                .HasMaxLength(255)
                .HasColumnName("alt_text");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
        });

        modelBuilder.Entity<MyService>(entity =>
        {
            entity.HasKey(e => e.UserSubId).HasName("My_Service_pkey");

            entity.ToTable("My_Service");

            entity.Property(e => e.UserSubId)
                .HasMaxLength(100)
                .HasColumnName("user_sub_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_date");
            entity.Property(e => e.ServiceId)
                .HasMaxLength(100)
                .HasColumnName("service_id");
            entity.Property(e => e.StartDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Service).WithMany(p => p.MyServices)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("fk_myservice_service");

            entity.HasOne(d => d.User).WithMany(p => p.MyServices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_myservice_user");
        });

        modelBuilder.Entity<MyVoucher>(entity =>
        {
            entity.HasKey(e => e.UserVoucherId).HasName("My_Voucher_pkey");

            entity.ToTable("My_Voucher");

            entity.Property(e => e.UserVoucherId)
                .HasMaxLength(100)
                .HasColumnName("user_voucher_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UsedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("used_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");
            entity.Property(e => e.VoucherId)
                .HasMaxLength(100)
                .HasColumnName("voucher_id");

            entity.HasOne(d => d.User).WithMany(p => p.MyVouchers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_myvoucher_user");

            entity.HasOne(d => d.Voucher).WithMany(p => p.MyVouchers)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("fk_myvoucher_voucher");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("Notification_pkey");

            entity.ToTable("Notification");

            entity.Property(e => e.NotificationId)
                .HasMaxLength(100)
                .HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.ReadAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("read_at");
            entity.Property(e => e.ReferenceId)
                .HasMaxLength(100)
                .HasColumnName("reference_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_notification_user");
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasKey(e => e.OfferId).HasName("Offer_pkey");

            entity.ToTable("Offer");

            entity.Property(e => e.OfferId)
                .HasMaxLength(100)
                .HasColumnName("offer_id");
            entity.Property(e => e.BuyerId)
                .HasMaxLength(100)
                .HasColumnName("buyer_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires_at");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.OfferPrice)
                .HasPrecision(18, 2)
                .HasColumnName("offer_price");
            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.Buyer).WithMany(p => p.Offers)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("fk_offer_buyer");

            entity.HasOne(d => d.Product).WithMany(p => p.Offers)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_offer_product");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("Order_pkey");

            entity.ToTable("Order");

            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .HasColumnName("order_id");
            entity.Property(e => e.AddressSnapshot).HasColumnName("address_snapshot");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(18, 2)
                .HasColumnName("discount_amount");
            entity.Property(e => e.ExpectedDeliveryTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expected_delivery_time");
            entity.Property(e => e.FinalAmount)
                .HasPrecision(18, 2)
                .HasColumnName("final_amount");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(50)
                .HasColumnName("order_code");
            entity.Property(e => e.ShippingFee)
                .HasPrecision(18, 2)
                .HasColumnName("shipping_fee");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_order_user");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("Order_Item_pkey");

            entity.ToTable("Order_Item");

            entity.Property(e => e.OrderItemId)
                .HasMaxLength(100)
                .HasColumnName("order_item_id");
            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .HasColumnName("order_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(18, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2)
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_orderitem_order");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_orderitem_product");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("Payment_pkey");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId)
                .HasMaxLength(100)
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .HasColumnName("order_id");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .HasColumnName("payment_method");
            entity.Property(e => e.ProviderTransactionId)
                .HasMaxLength(255)
                .HasColumnName("provider_transaction_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_payment_order");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_payment_user");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("Product_pkey");

            entity.ToTable("Product");

            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(100)
                .HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.HeightCm).HasColumnName("height_cm");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.LengthCm).HasColumnName("length_cm");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.SellerId)
                .HasMaxLength(100)
                .HasColumnName("seller_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status");
            entity.Property(e => e.StockQuantity).HasColumnName("stock_quantity");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.WeightGram).HasColumnName("weight_gram");
            entity.Property(e => e.WidthCm).HasColumnName("width_cm");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_product_category");

            entity.HasOne(d => d.Seller).WithMany(p => p.Products)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_product_seller");
        });

        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.HasKey(e => e.ProductAttributeId).HasName("Product_Attribute_pkey");

            entity.ToTable("Product_Attribute");

            entity.Property(e => e.ProductAttributeId)
                .HasMaxLength(100)
                .HasColumnName("product_attribute_id");
            entity.Property(e => e.AttributeId)
                .HasMaxLength(100)
                .HasColumnName("attribute_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.Attribute).WithMany(p => p.ProductAttributes)
                .HasForeignKey(d => d.AttributeId)
                .HasConstraintName("fk_prod_attr_attribute");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductAttributes)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_prod_attr_product");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.ImageId }).HasName("Product_Image_pkey");

            entity.ToTable("Product_Image");

            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.ImageId)
                .HasMaxLength(100)
                .HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsMain).HasColumnName("is_main");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.Image).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_prodimg_image");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_prodimg_product");
        });

        modelBuilder.Entity<RefundRequest>(entity =>
        {
            entity.HasKey(e => e.RefundRequestId).HasName("Refund_Request_pkey");

            entity.ToTable("Refund_Request");

            entity.Property(e => e.RefundRequestId)
                .HasMaxLength(100)
                .HasColumnName("refund_request_id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.BankAccountHolder)
                .HasMaxLength(255)
                .HasColumnName("bank_account_holder");
            entity.Property(e => e.BankAccountNumber)
                .HasMaxLength(100)
                .HasColumnName("bank_account_number");
            entity.Property(e => e.BankName)
                .HasMaxLength(255)
                .HasColumnName("bank_name");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.RejectReason).HasColumnName("reject_reason");
            entity.Property(e => e.RequestedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("requested_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefundRequests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_refund_user");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("Review_pkey");

            entity.ToTable("Review");

            entity.HasIndex(e => new { e.OrderId, e.ReviewerId }, "Review_order_id_reviewer_id_key").IsUnique();

            entity.Property(e => e.ReviewId)
                .HasMaxLength(100)
                .HasColumnName("review_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .HasColumnName("order_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReviewerId)
                .HasMaxLength(100)
                .HasColumnName("reviewer_id");
            entity.Property(e => e.SellerId)
                .HasMaxLength(100)
                .HasColumnName("seller_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Order).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_review_order");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ReviewReviewers)
                .HasForeignKey(d => d.ReviewerId)
                .HasConstraintName("fk_review_reviewer");

            entity.HasOne(d => d.Seller).WithMany(p => p.ReviewSellers)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_review_seller");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("Role_pkey");

            entity.ToTable("Role");

            entity.Property(e => e.RoleId)
                .HasMaxLength(100)
                .HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ServiceSubscription>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("Service_Subscription_pkey");

            entity.ToTable("Service_Subscription");

            entity.Property(e => e.ServiceId)
                .HasMaxLength(100)
                .HasColumnName("service_id");
            entity.Property(e => e.BenefitsDescription).HasColumnName("benefits_description");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.TargetRole)
                .HasMaxLength(50)
                .HasColumnName("target_role");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("User_pkey");

            entity.ToTable("User");

            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserFavorite>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("User_Favorite_pkey");

            entity.ToTable("User_Favorite");

            entity.Property(e => e.FavoriteId)
                .HasMaxLength(100)
                .HasColumnName("favorite_id");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(100)
                .HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Category).WithMany(p => p.UserFavorites)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_favorite_category");

            entity.HasOne(d => d.User).WithMany(p => p.UserFavorites)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_favorite_user");
        });

        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("User_Follow_pkey");

            entity.ToTable("User_Follow");

            entity.Property(e => e.FollowId)
                .HasMaxLength(100)
                .HasColumnName("follow_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FollowedUserId)
                .HasMaxLength(100)
                .HasColumnName("followed_user_id");
            entity.Property(e => e.FollowerId)
                .HasMaxLength(100)
                .HasColumnName("follower_id");

            entity.HasOne(d => d.FollowedUser).WithMany(p => p.UserFollowFollowedUsers)
                .HasForeignKey(d => d.FollowedUserId)
                .HasConstraintName("fk_follow_followed");

            entity.HasOne(d => d.Follower).WithMany(p => p.UserFollowFollowers)
                .HasForeignKey(d => d.FollowerId)
                .HasConstraintName("fk_follow_follower");
        });

        modelBuilder.Entity<UserSearch>(entity =>
        {
            entity.HasKey(e => e.SearchId).HasName("User_Search_pkey");

            entity.ToTable("User_Search");

            entity.Property(e => e.SearchId)
                .HasMaxLength(100)
                .HasColumnName("search_id");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(100)
                .HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Keyword)
                .HasMaxLength(255)
                .HasColumnName("keyword");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Category).WithMany(p => p.UserSearches)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_search_category");

            entity.HasOne(d => d.User).WithMany(p => p.UserSearches)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_search_user");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("Voucher_pkey");

            entity.ToTable("Voucher");

            entity.Property(e => e.VoucherId)
                .HasMaxLength(100)
                .HasColumnName("voucher_id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasPrecision(18, 2)
                .HasColumnName("discount_value");
            entity.Property(e => e.ExpirationDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiration_date");
            entity.Property(e => e.MaxDiscountValue)
                .HasPrecision(18, 2)
                .HasColumnName("max_discount_value");
            entity.Property(e => e.MinOrderValue)
                .HasPrecision(18, 2)
                .HasColumnName("min_order_value");
            entity.Property(e => e.StartDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("Wishlist_pkey");

            entity.ToTable("Wishlist");

            entity.Property(e => e.WishlistId)
                .HasMaxLength(100)
                .HasColumnName("wishlist_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_wishlist_user");
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.WishlistItemId).HasName("Wishlist_Item_pkey");

            entity.ToTable("Wishlist_Item");

            entity.Property(e => e.WishlistItemId)
                .HasMaxLength(100)
                .HasColumnName("wishlist_item_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.WishlistId)
                .HasMaxLength(100)
                .HasColumnName("wishlist_id");

            entity.HasOne(d => d.Product).WithMany(p => p.WishlistItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_wishitem_product");

            entity.HasOne(d => d.Wishlist).WithMany(p => p.WishlistItems)
                .HasForeignKey(d => d.WishlistId)
                .HasConstraintName("fk_wishitem_wishlist");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
