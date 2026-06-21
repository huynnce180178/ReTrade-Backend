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

    public virtual DbSet<Account> Account { get; set; }

    public virtual DbSet<AccountRole> AccountRole { get; set; }

    public virtual DbSet<Address> Address { get; set; }

    public virtual DbSet<Attributes> Attributes { get; set; }

    public virtual DbSet<Auction> Auction { get; set; }

    public virtual DbSet<AuctionDeposit> AuctionDeposit { get; set; }

    public virtual DbSet<Banner> Banner { get; set; }

    public virtual DbSet<Bid> Bid { get; set; }

    public virtual DbSet<Category> Category { get; set; }

    public virtual DbSet<CategoryImage> CategoryImage { get; set; }

    public virtual DbSet<Chat> Chat { get; set; }

    public virtual DbSet<ChatRoom> ChatRoom { get; set; }

    public virtual DbSet<Image> Image { get; set; }

    public virtual DbSet<MyService> MyService { get; set; }

    public virtual DbSet<MyVoucher> MyVoucher { get; set; }

    public virtual DbSet<Notification> Notification { get; set; }

    public virtual DbSet<Offer> Offer { get; set; }

    public virtual DbSet<Order> Order { get; set; }

    public virtual DbSet<Payment> Payment { get; set; }

    public virtual DbSet<Product> Product { get; set; }

    public virtual DbSet<ProductAttribute> ProductAttribute { get; set; }

    public virtual DbSet<ProductImage> ProductImage { get; set; }

    public virtual DbSet<RefundRequest> RefundRequest { get; set; }

    public virtual DbSet<Review> Review { get; set; }

    public virtual DbSet<Role> Role { get; set; }

    public virtual DbSet<ServiceSubscription> ServiceSubscription { get; set; }

    public virtual DbSet<User> User { get; set; }

    public virtual DbSet<UserFavorite> UserFavorite { get; set; }

    public virtual DbSet<UserFollow> UserFollow { get; set; }

    public virtual DbSet<UserReport> UserReport { get; set; }

    public virtual DbSet<UserSearch> UserSearch { get; set; }

    public virtual DbSet<Voucher> Voucher { get; set; }

    public virtual DbSet<Wishlist> Wishlist { get; set; }

    public virtual DbSet<WishlistItem> WishlistItem { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("account_pkey");

            entity.ToTable("account");

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
            entity.Property(e => e.MustChangePassword)
                .HasDefaultValue(false)
                .HasColumnName("must_change_password");
            entity.Property(e => e.IsPasswordSet)
                .HasDefaultValue(true)
                .HasColumnName("is_password_set");
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

            entity.HasOne(d => d.User).WithMany(p => p.Account)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_account_user");
        });

        modelBuilder.Entity<AccountRole>(entity =>
        {
            entity.HasKey(e => new { e.AccountId, e.RoleId }).HasName("account_role_pkey");

            entity.ToTable("account_role");

            entity.Property(e => e.AccountId)
                .HasMaxLength(100)
                .HasColumnName("account_id");
            entity.Property(e => e.RoleId)
                .HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountRole)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ar_account");

            entity.HasOne(d => d.Role).WithMany(p => p.AccountRole)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ar_role");
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("address_pkey");

            entity.ToTable("address");

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
            entity.Property(e => e.ReceiverName)
                .HasMaxLength(100)
                .HasColumnName("receiver_name");
            entity.Property(e => e.ReceiverPhone)
                .HasMaxLength(30)
                .HasColumnName("receiver_phone");
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

            entity.HasOne(d => d.User).WithMany(p => p.Address)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_address_user");
        });

        modelBuilder.Entity<Attributes>(entity =>
        {
            entity.HasKey(e => e.AttributeId).HasName("attributes_pkey");

            entity.ToTable("attributes");

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
                .HasConstraintName("fk_attr_category");
        });

        modelBuilder.Entity<Auction>(entity =>
        {
            entity.HasKey(e => e.AuctionId).HasName("auction_pkey");

            entity.ToTable("auction");

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

            entity.HasOne(d => d.Product).WithMany(p => p.Auction)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_auction_product");

            entity.HasOne(d => d.Seller).WithMany(p => p.AuctionSeller)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_auction_seller");

            entity.HasOne(d => d.Winner).WithMany(p => p.AuctionWinner)
                .HasForeignKey(d => d.WinnerId)
                .HasConstraintName("fk_auction_winner");
        });

        modelBuilder.Entity<AuctionDeposit>(entity =>
        {
            entity.HasKey(e => e.AuctionDepositId).HasName("auction_deposit_pkey");

            entity.ToTable("auction_deposit");

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

            entity.HasOne(d => d.Auction).WithMany(p => p.AuctionDeposit)
                .HasForeignKey(d => d.AuctionId)
                .HasConstraintName("fk_ad_auction");

            entity.HasOne(d => d.User).WithMany(p => p.AuctionDeposit)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_ad_user");
        });

        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("banner_pkey");

            entity.ToTable("banner");

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
            entity.HasKey(e => e.BidId).HasName("bid_pkey");

            entity.ToTable("bid");

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

            entity.HasOne(d => d.Auction).WithMany(p => p.Bid)
                .HasForeignKey(d => d.AuctionId)
                .HasConstraintName("fk_bid_auction");

            entity.HasOne(d => d.User).WithMany(p => p.Bid)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_bid_user");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("category_pkey");

            entity.ToTable("category");

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
            entity.Property(e => e.ParentId)
                .HasMaxLength(100)
                .HasColumnName("parent_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("fk_category_parent");
        });

        modelBuilder.Entity<CategoryImage>(entity =>
        {
            entity.HasKey(e => new { e.CategoryId, e.ImageId }).HasName("category_image_pkey");

            entity.ToTable("category_image");

            entity.Property(e => e.CategoryId)
                .HasMaxLength(100)
                .HasColumnName("category_id");
            entity.Property(e => e.ImageId)
                .HasMaxLength(100)
                .HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Category).WithMany(p => p.CategoryImage)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ci_category");

            entity.HasOne(d => d.Image).WithMany(p => p.CategoryImage)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ci_image");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("chat_pkey");

            entity.ToTable("chat");

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

            entity.HasOne(d => d.Room).WithMany(p => p.Chat)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("fk_chat_room");

            entity.HasOne(d => d.Sender).WithMany(p => p.Chat)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("fk_chat_sender");
        });

        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("chat_room_pkey");

            entity.ToTable("chat_room");

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

            entity.HasOne(d => d.Buyer).WithMany(p => p.ChatRoomBuyer)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("fk_cr_buyer");

            entity.HasOne(d => d.Product).WithMany(p => p.ChatRoom)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_cr_product");

            entity.HasOne(d => d.Seller).WithMany(p => p.ChatRoomSeller)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_cr_seller");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("image_pkey");

            entity.ToTable("image");

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
            entity.HasKey(e => e.UserSubId).HasName("my_service_pkey");

            entity.ToTable("my_service");

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

            entity.HasOne(d => d.Service).WithMany(p => p.MyService)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("fk_ms_service");

            entity.HasOne(d => d.User).WithMany(p => p.MyService)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_ms_user");
        });

        modelBuilder.Entity<MyVoucher>(entity =>
        {
            entity.HasKey(e => e.UserVoucherId).HasName("my_voucher_pkey");

            entity.ToTable("my_voucher");

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

            entity.HasOne(d => d.User).WithMany(p => p.MyVoucher)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_mv_user");

            entity.HasOne(d => d.Voucher).WithMany(p => p.MyVoucher)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("fk_mv_voucher");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notification_pkey");

            entity.ToTable("notification");

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

            entity.HasOne(d => d.User).WithMany(p => p.Notification)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_notify_user");
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasKey(e => e.OfferId).HasName("offer_pkey");

            entity.ToTable("offer");

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

            entity.HasOne(d => d.Buyer).WithMany(p => p.Offer)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("fk_offer_buyer");

            entity.HasOne(d => d.Product).WithMany(p => p.Offer)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_offer_product");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("Order_pkey");

            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .HasColumnName("order_id");
            entity.Property(e => e.AddressSnapshot).HasColumnName("address_snapshot");
            entity.Property(e => e.AuctionId)
                .HasMaxLength(100)
                .HasColumnName("auction_id");
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
            entity.Property(e => e.OfferId)
                .HasMaxLength(100)
                .HasColumnName("offer_id");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(50)
                .HasColumnName("order_code");
            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ReturnReason).HasColumnName("return_reason");
            entity.Property(e => e.SellerId)
                .HasMaxLength(100)
                .HasColumnName("seller_id");
            entity.Property(e => e.ShippingFee)
                .HasPrecision(18, 2)
                .HasColumnName("shipping_fee");
            entity.Property(e => e.ShippingProvider)
                .HasMaxLength(100)
                .HasColumnName("shipping_provider");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.TrackingCode)
                .HasMaxLength(100)
                .HasColumnName("tracking_code");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2)
                .HasColumnName("unit_price");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .HasColumnName("user_id");
            entity.Property(e => e.VoucherId)
                .HasMaxLength(100)
                .HasColumnName("voucher_id");

            entity.HasOne(d => d.Auction).WithMany(p => p.Order)
                .HasForeignKey(d => d.AuctionId)
                .HasConstraintName("fk_order_auction");

            entity.HasOne(d => d.Offer).WithMany(p => p.Order)
                .HasForeignKey(d => d.OfferId)
                .HasConstraintName("fk_order_offer");

            entity.HasOne(d => d.Product).WithMany(p => p.Order)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_order_product");

            entity.HasOne(d => d.Seller).WithMany(p => p.OrderSeller)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_order_seller");

            entity.HasOne(d => d.User).WithMany(p => p.OrderUser)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_order_user");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Order)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("fk_order_voucher");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payment_pkey");

            entity.ToTable("payment");

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
            entity.Property(e => e.ServiceId)
                .HasMaxLength(100)
                .HasColumnName("service_id");
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

            entity.HasOne(d => d.Order).WithMany(p => p.Payment)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_payment_order");

            entity.HasOne(d => d.User).WithMany(p => p.Payment)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_payment_user");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("product_pkey");

            entity.ToTable("product");

            entity.Property(e => e.ProductId)
                .HasMaxLength(100)
                .HasColumnName("product_id");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(100)
                .HasColumnName("category_id");
            entity.Property(e => e.Condition)
                .HasMaxLength(50)
                .HasColumnName("condition");
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

            entity.HasOne(d => d.Category).WithMany(p => p.Product)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_product_category");

            entity.HasOne(d => d.Seller).WithMany(p => p.Product)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_product_seller");
        });

        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.HasKey(e => e.ProductAttributeId).HasName("product_attribute_pkey");

            entity.ToTable("product_attribute");

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

            entity.HasOne(d => d.Attribute).WithMany(p => p.ProductAttribute)
                .HasForeignKey(d => d.AttributeId)
                .HasConstraintName("fk_pa_attr");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductAttribute)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_pa_product");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.ImageId }).HasName("product_image_pkey");

            entity.ToTable("product_image");

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

            entity.HasOne(d => d.Image).WithMany(p => p.ProductImage)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pi_image");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImage)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pi_product");
        });

        modelBuilder.Entity<RefundRequest>(entity =>
        {
            entity.HasKey(e => e.RefundRequestId).HasName("refund_request_pkey");

            entity.ToTable("refund_request");

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
            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .HasColumnName("order_id");
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

            entity.HasOne(d => d.Order).WithMany(p => p.RefundRequest)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_rr_order");

            entity.HasOne(d => d.User).WithMany(p => p.RefundRequest)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_rr_user");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("review_pkey");

            entity.ToTable("review");

            entity.HasIndex(e => new { e.OrderId, e.ReviewerId }, "review_order_id_reviewer_id_key").IsUnique();

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

            entity.HasOne(d => d.Order).WithMany(p => p.Review)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_review_order");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ReviewReviewer)
                .HasForeignKey(d => d.ReviewerId)
                .HasConstraintName("fk_review_reviewer");

            entity.HasOne(d => d.Seller).WithMany(p => p.ReviewSeller)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_review_seller");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.ToTable("role");

            entity.Property(e => e.RoleId)
                .HasMaxLength(100)
                .HasColumnName("role_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<ServiceSubscription>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("service_subscription_pkey");

            entity.ToTable("service_subscription");

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
            entity.Property(e => e.FlagCount)
                .HasDefaultValue(0)
                .HasColumnName("flag_count");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserFavorite>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("user_favorite_pkey");

            entity.ToTable("user_favorite");

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

            entity.HasOne(d => d.Category).WithMany(p => p.UserFavorite)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_ufav_category");

            entity.HasOne(d => d.User).WithMany(p => p.UserFavorite)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_ufav_user");
        });

        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("user_follow_pkey");

            entity.ToTable("user_follow");

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

            entity.HasOne(d => d.FollowedUser).WithMany(p => p.UserFollowFollowedUser)
                .HasForeignKey(d => d.FollowedUserId)
                .HasConstraintName("fk_uf_followed");

            entity.HasOne(d => d.Follower).WithMany(p => p.UserFollowFollower)
                .HasForeignKey(d => d.FollowerId)
                .HasConstraintName("fk_uf_follower");
        });

        modelBuilder.Entity<UserReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("user_report_pkey");

            entity.ToTable("user_report");

            entity.Property(e => e.ReportId)
                .HasMaxLength(100)
                .HasColumnName("report_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Reason)
                .HasMaxLength(100)
                .HasColumnName("reason");
            entity.Property(e => e.ReporterId)
                .HasMaxLength(100)
                .HasColumnName("reporter_id");
            entity.Property(e => e.ReviewedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy)
                .HasMaxLength(100)
                .HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TargetId)
                .HasMaxLength(100)
                .HasColumnName("target_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(30)
                .HasColumnName("target_type");

            entity.HasOne(d => d.Reporter).WithMany(p => p.UserReportReporter)
                .HasForeignKey(d => d.ReporterId)
                .HasConstraintName("fk_ur_reporter");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.UserReportReviewedByNavigation)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("fk_ur_reviewer");
        });

        modelBuilder.Entity<UserSearch>(entity =>
        {
            entity.HasKey(e => e.SearchId).HasName("user_search_pkey");

            entity.ToTable("user_search");

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

            entity.HasOne(d => d.Category).WithMany(p => p.UserSearch)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_us_category");

            entity.HasOne(d => d.User).WithMany(p => p.UserSearch)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_us_user");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("voucher_pkey");

            entity.ToTable("voucher");

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
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SellerId)
                .HasMaxLength(100)
                .HasColumnName("seller_id");
            entity.Property(e => e.StartDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Seller).WithMany(p => p.Voucher)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("fk_voucher_seller");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("wishlist_pkey");

            entity.ToTable("wishlist");

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

            entity.HasOne(d => d.User).WithMany(p => p.Wishlist)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_wishlist_user");
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.WishlistItemId).HasName("wishlist_item_pkey");

            entity.ToTable("wishlist_item");

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

            entity.HasOne(d => d.Product).WithMany(p => p.WishlistItem)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_wi_product");

            entity.HasOne(d => d.Wishlist).WithMany(p => p.WishlistItem)
                .HasForeignKey(d => d.WishlistId)
                .HasConstraintName("fk_wi_wishlist");
        });
        OnModelCreatingPartial(modelBuilder);
    }
        

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}
