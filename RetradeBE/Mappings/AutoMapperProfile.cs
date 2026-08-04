using AutoMapper;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.DTOs.Admin;
using System.Linq;

namespace RetradeBE.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // RegisterDto -> User
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => "https://res.cloudinary.com/dx0hrokek/image/upload/v1780673207/avt-emty_wwnzba.jpg"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // RegisterDto -> Account
            CreateMap<RegisterDto, Account>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => RetradeBE.Models.Enums.AccountStatusEnum.Pending.ToString()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Will be hashed manually in service


            // Category Mappings
            CreateMap<Category, CategoryResponseDto>()
                .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.Attributes
                    .OrderBy(a => a.DisplayOrder)))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.CategoryImage.OrderBy(ci => ci.CreatedAt).Select(ci => ci.Image.ImageUrl).FirstOrDefault()));

            // Account -> UserListDto (admin user list)
            CreateMap<Account, UserListDto>()
                .ForMember(dest => dest.PrimaryRole, opt => opt.MapFrom(src =>
                    src.AccountRole
                        .OrderBy(ar => ar.CreatedAt)
                        .Select(ar => ar.Role != null ? ar.Role.Name : null)
                        .FirstOrDefault()))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User != null ? src.User.FirstName : null))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User != null ? src.User.LastName : null))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : null))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null));

            // Attributes -> AttributeDto
            CreateMap<Attributes, AttributeDto>();

            // AttributeCreateDto -> Attributes
            CreateMap<AttributeCreateDto, Attributes>()
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

            // AttributeUpdateDto -> Attributes
            CreateMap<AttributeUpdateDto, Attributes>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            CreateMap<Order, PurchaseListDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src => src.Product != null
                    ? src.Product.ProductImage
                        .Where(pi => pi.IsMain == true)
                        .Select(pi => pi.Image.ImageUrl)
                        .FirstOrDefault()
                      ?? src.Product.ProductImage
                        .OrderBy(pi => pi.SortOrder)
                        .Select(pi => pi.Image.ImageUrl)
                        .FirstOrDefault()
                    : null))
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.Seller != null ? (src.Seller.FirstName + " " + src.Seller.LastName).Trim() : null))
                .ForMember(dest => dest.SellerEmail, opt => opt.MapFrom(src => src.Seller != null ? src.Seller.Email : null))
                .ForMember(dest => dest.SellerPhone, opt => opt.MapFrom(src => src.Seller != null ? src.Seller.Phone : null))
                .ForMember(dest => dest.HasReview, opt => opt.MapFrom(src => src.Review.Any()));

            CreateMap<Order, PurchaseDetailDto>()
                .IncludeBase<Order, PurchaseListDto>()
                .ForMember(dest => dest.BuyerId, opt => opt.MapFrom(src => src.BuyerId))
                .ForMember(dest => dest.BuyerName, opt => opt.MapFrom(src => src.Buyer != null ? (src.Buyer.FirstName + " " + src.Buyer.LastName).Trim() : null))
                .ForMember(dest => dest.BuyerEmail, opt => opt.MapFrom(src => src.Buyer != null ? src.Buyer.Email : null))
                .ForMember(dest => dest.BuyerPhone, opt => opt.MapFrom(src => src.Buyer != null ? src.Buyer.Phone : null))
                .ForMember(dest => dest.HasReview, opt => opt.MapFrom(src => src.Review.Any()))
                .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payment.OrderByDescending(p => p.CreatedAt)));

            CreateMap<Payment, PaymentSummaryDto>();
            CreateMap<Review, ReviewResponseDto>();
            CreateMap<Report, ReportDto>()
                .ForMember(dest => dest.ReporterName, opt => opt.MapFrom(src => src.Reporter == null
                    ? null
                    : string.IsNullOrWhiteSpace((src.Reporter.FirstName + " " + src.Reporter.LastName).Trim())
                        ? src.Reporter.Email
                        : (src.Reporter.FirstName + " " + src.Reporter.LastName).Trim()));
            CreateMap<Report, ReportListDto>()
                .ForMember(dest => dest.ReporterName, opt => opt.MapFrom(src => src.Reporter == null
                    ? null
                    : string.IsNullOrWhiteSpace((src.Reporter.FirstName + " " + src.Reporter.LastName).Trim())
                        ? src.Reporter.Email
                        : (src.Reporter.FirstName + " " + src.Reporter.LastName).Trim()));
            CreateMap<Report, ReportDetailDto>()
                .ForMember(dest => dest.ReporterName, opt => opt.MapFrom(src => src.Reporter == null
                    ? null
                    : string.IsNullOrWhiteSpace((src.Reporter.FirstName + " " + src.Reporter.LastName).Trim())
                        ? src.Reporter.Email
                        : (src.Reporter.FirstName + " " + src.Reporter.LastName).Trim()))
                .ForMember(dest => dest.Review, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.Ignore())
                .ForMember(dest => dest.Buyer, opt => opt.Ignore())
                .ForMember(dest => dest.Seller, opt => opt.Ignore());
            CreateMap<Review, ReportReviewDetailDto>();
            CreateMap<Order, ReportOrderDetailDto>();
            CreateMap<User, ReportUserDetailDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace((src.FirstName + " " + src.LastName).Trim())
                        ? src.Email
                        : (src.FirstName + " " + src.LastName).Trim()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsDeleted == true ? "Deleted" : "Active"));
            CreateMap<User, FlaggedUserDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace((src.FirstName + " " + src.LastName).Trim())
                        ? src.Email
                        : (src.FirstName + " " + src.LastName).Trim()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsDeleted == true ? "Deleted" : "Active"))
                .ForMember(dest => dest.Reports, opt => opt.Ignore());
            //Attribute -> AttributeDTO
            CreateMap<Role, RoleDto>();

            // UserSearch Mappings
            CreateMap<UserSearch, UserSearchResponseDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));
            CreateMap<UserSearchCreateDto, UserSearch>();

            // UserFavorite Mappings
            CreateMap<UserFavorite, UserFavoriteResponseDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.CategoryImageUrl, opt => opt.MapFrom(src => src.Category != null
                    ? src.Category.CategoryImage.OrderByDescending(ci => ci.CreatedAt).Select(ci => ci.Image != null ? ci.Image.ImageUrl : null).FirstOrDefault()
                    : null));
            CreateMap<UserFavoriteCreateDto, UserFavorite>();

            // Profile Mappings
            CreateMap<Address, AddressDto>()
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
                .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.Street));

            CreateMap<Account, ProfileDetailDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.UserId))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username ?? string.Empty))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User.Phone))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.User.IsDeleted))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.User.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.User.UpdatedAt))
                .ForMember(dest => dest.DefaultAddress, opt => opt.Ignore())
                .ForMember(dest => dest.Addresses, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            CreateMap<User, SellerDetailDto>()
                .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.AccountId, opt => opt.Ignore())
                .ForMember(dest => dest.Username, opt => opt.Ignore())
                .ForMember(dest => dest.FollowersCount, opt => opt.Ignore())
                .ForMember(dest => dest.FollowingCount, opt => opt.Ignore())
                .ForMember(dest => dest.ProductCount, opt => opt.Ignore())
                .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewCount, opt => opt.Ignore())
                .ForMember(dest => dest.RatingStats, opt => opt.Ignore())
                .ForMember(dest => dest.IsSeller, opt => opt.Ignore())
                .ForMember(dest => dest.IsFollowing, opt => opt.Ignore())
                .ForMember(dest => dest.IsOwnSeller, opt => opt.Ignore())
                .ForMember(dest => dest.CanFollow, opt => opt.Ignore())
                .ForMember(dest => dest.DefaultAddress, opt => opt.Ignore());

            // Wishlist Mappings
            CreateMap<Wishlist, WishlistDetailDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.WishlistItem));

            CreateMap<WishlistItem, WishlistItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Product.Price))
                .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.Product.StockQuantity))
                .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.Product.Condition))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Product.Status))
                .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.Product.SellerId))
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.Product.Seller != null ? $"{src.Product.Seller.FirstName} {src.Product.Seller.LastName}".Trim() : null))
                .ForMember(dest => dest.AddedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => 
                    src.Product.ProductImage.Where(pi => pi.IsMain == true).Select(pi => pi.Image.ImageUrl).FirstOrDefault() ?? 
                    src.Product.ProductImage.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.ImageUrl).FirstOrDefault()));

            // MyVoucher Mappings
            CreateMap<MyVoucher, MyVoucherDto>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.Code : null))
                .ForMember(dest => dest.DiscountType, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.DiscountType : null))
                .ForMember(dest => dest.DiscountValue, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.DiscountValue : null))
                .ForMember(dest => dest.MinOrderValue, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.MinOrderValue : null))
                .ForMember(dest => dest.MaxDiscountValue, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.MaxDiscountValue : null))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.StartDate : null))
                .ForMember(dest => dest.ExpirationDate, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.ExpirationDate : null))
                .ForMember(dest => dest.VoucherStatus, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.Status : null))
                .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.SellerId : null))
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.Voucher != null && src.Voucher.Seller != null ? src.Voucher.Seller.FirstName + " " + src.Voucher.Seller.LastName : null));
        }
    }
}

