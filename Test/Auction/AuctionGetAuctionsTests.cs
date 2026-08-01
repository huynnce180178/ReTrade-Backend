using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Mappings;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;
using Test;

namespace Test.AuctionTests
{
    public class AuctionGetAuctionsTests
    {
        private readonly Mock<IAuctionRepository> _auctionRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IHubContext<AuctionHub>> _auctionHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly AuctionService _service;

        public AuctionGetAuctionsTests()
        {
            _auctionRepository = new Mock<IAuctionRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _context = new Mock<AppDbContext>();
            _paymentService = new Mock<IPaymentService>();
            _auctionHub = new Mock<IHubContext<AuctionHub>>();
            _notificationService = new Mock<INotificationService>();

            // Cấu hình mapper với NullLoggerFactory
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = configuration.CreateMapper();

            _service = new AuctionService(
                _auctionRepository.Object,
                _accountRepository.Object,
                _context.Object,
                _paymentService.Object,
                _auctionHub.Object,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task GetAuctionsAsync_ShouldReturnPagedAuctions_WhenQueryIsEmpty()
        {
            // Arrange
            var query = new AuctionQueryDto();
            var baseTime = DateTime.UtcNow.AddHours(7);
            
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            var seller = new User { UserId = "seller_1", FirstName = "John", LastName = "Doe" };
            
            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_1",
                    ProductId = "prod_1",
                    SellerId = "seller_1",
                    StartingPrice = 100,
                    CurrentPrice = 100,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(2),
                    Status = "Ongoing",
                    CreatedAt = baseTime.AddDays(-1),
                    Product = new Product
                    {
                        ProductId = "prod_1",
                        Name = "Product 1",
                        Category = category,
                        ProductImage = new List<ProductImage>()
                    },
                    Seller = seller,
                    Bid = new List<Bid>()
                }
            };

            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetAuctionsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].AuctionId.Should().Be("auc_1");
            result.Items[0].ProductName.Should().Be("Product 1");
            result.Items[0].SellerName.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetAuctionsAsync_ShouldFilterBySearchTermAndSellerId_WhenTheseFiltersAreProvided()
        {
            // Arrange
            var query = new AuctionQueryDto 
            { 
                SearchTerm = "laptop", 
                SellerId = "seller_1",
                SortBy = "price_desc"
            };
            var baseTime = DateTime.UtcNow.AddHours(7);
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_match",
                    ProductId = "prod_1",
                    SellerId = "seller_1",
                    StartingPrice = 250,
                    CurrentPrice = 250,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(2),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "prod_1", Name = "Gaming Laptop", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_wrong_seller",
                    ProductId = "prod_2",
                    SellerId = "seller_2",
                    StartingPrice = 300,
                    CurrentPrice = 300,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(2),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "prod_2", Name = "Office Laptop", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetAuctionsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].AuctionId.Should().Be("auc_match");
        }

        [Fact]
        public async Task GetAuctionsAsync_ShouldFilterBySearchTermForSeller_WhenSearchTermMatchesSellerName()
        {
            // Arrange
            var query = new AuctionQueryDto { SearchTerm = "smith" };
            var baseTime = DateTime.UtcNow.AddHours(7);
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_1",
                    ProductId = "prod_1",
                    SellerId = "seller_1",
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(2),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "prod_1", Category = category, ProductImage = new List<ProductImage>() },
                    Seller = new User { UserId = "seller_1", FirstName = "Alice", LastName = "Smith" },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_2",
                    ProductId = "prod_2",
                    SellerId = "seller_2",
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(2),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "prod_2", Category = category, ProductImage = new List<ProductImage>() },
                    Seller = new User { UserId = "seller_2", FirstName = "Bob", LastName = "Jones" },
                    Bid = new List<Bid>()
                }
            };

            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetAuctionsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].SellerName.Should().Be("Alice Smith");
        }

        [Fact]
        public async Task GetAuctionsAsync_ShouldFilterByStatusUpcomingAndSortByStartingSoon_WhenUpcomingStatusIsRequested()
        {
            // Arrange
            var query = new AuctionQueryDto { Status = "Upcoming", SortBy = "starting_soon" };
            var baseTime = DateTime.UtcNow.AddHours(7);
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_later",
                    StartTime = baseTime.AddHours(3),
                    EndTime = baseTime.AddHours(6),
                    Status = "Upcoming",
                    Product = new Product { ProductId = "p1", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_soon",
                    StartTime = baseTime.AddHours(1),
                    EndTime = baseTime.AddHours(5),
                    Status = "Upcoming",
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetAuctionsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items[0].AuctionId.Should().Be("auc_soon");
            result.Items[1].AuctionId.Should().Be("auc_later");
        }

        [Fact]
        public async Task GetAuctionsAsync_ShouldFilterByStatusOngoingAndSortByPriceAsc_WhenOngoingStatusIsRequested()
        {
            // Arrange
            var query = new AuctionQueryDto { Status = "Ongoing", SortBy = "price_asc" };
            var baseTime = DateTime.UtcNow.AddHours(7);
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_expensive",
                    StartingPrice = 500,
                    CurrentPrice = 500,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "p1", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_cheap",
                    StartingPrice = 100,
                    CurrentPrice = 100,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetAuctionsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items[0].AuctionId.Should().Be("auc_cheap");
            result.Items[1].AuctionId.Should().Be("auc_expensive");
        }

        [Fact]
        public async Task GetAuctionsAsync_ShouldFilterByStatusEndedAndSortByEndingSoon_WhenEndedStatusIsRequested()
        {
            // Arrange
            var query = new AuctionQueryDto { Status = "Ended", IncludeEnded = true, SortBy = "ending_soon" };
            var baseTime = DateTime.UtcNow.AddHours(7);
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_ended_long_ago",
                    StartTime = baseTime.AddHours(-5),
                    EndTime = baseTime.AddHours(-3),
                    Status = "Ended",
                    Product = new Product { ProductId = "p1", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_ended_recently",
                    StartTime = baseTime.AddHours(-5),
                    EndTime = baseTime.AddHours(-1),
                    Status = "Ended",
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetAuctionsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items[0].AuctionId.Should().Be("auc_ended_long_ago");
            result.Items[1].AuctionId.Should().Be("auc_ended_recently");
        }

        [Fact]
        public async Task GetAuctionsAsync_ShouldFilterByCustomStatusAndSortByOldest_WhenCustomStatusAndOldestAreRequested()
        {
            // Arrange
            var query = new AuctionQueryDto { Status = "Cancelled", IncludeEnded = true, SortBy = "oldest" };
            var baseTime = DateTime.UtcNow.AddHours(7);
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_new",
                    Status = "Cancelled",
                    CreatedAt = baseTime.AddMinutes(-5),
                    Product = new Product { ProductId = "p1", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_old",
                    Status = "Cancelled",
                    CreatedAt = baseTime.AddMinutes(-20),
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetAuctionsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items[0].AuctionId.Should().Be("auc_old");
            result.Items[1].AuctionId.Should().Be("auc_new");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetAuctionsAsync_ShouldThrowNullReferenceException_WhenQueryIsNull()
        {
            // Act & Assert
            await _service.Invoking(s => s.GetAuctionsAsync(null!))
                .Should().ThrowAsync<NullReferenceException>();
        }

        [Fact]
        public async Task GetAuctionsAsync_ShouldThrowException_WhenDatabaseQueryThrowsException()
        {
            // Arrange
            var query = new AuctionQueryDto();
            _auctionRepository.Setup(x => x.Query()).Throws(new Exception("Database connection failed"));

            // Act & Assert
            await _service.Invoking(s => s.GetAuctionsAsync(query))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Database connection failed");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetAuctionsAsync_ShouldHandlePaginationBoundariesAndExclusions_WhenPagingRequestedAndDeletedProductsExist()
        {
            // Arrange
            var query = new AuctionQueryDto { Page = 2, PageSize = 1, SellerId = null };
            var baseTime = DateTime.UtcNow.AddHours(7);
            var categoryActive = new Category { CategoryId = "cat_active", Status = "Active" };
            var categoryInactive = new Category { CategoryId = "cat_inactive", Status = "Inactive" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_deleted_prod",
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    CreatedAt = baseTime.AddMinutes(-5),
                    Product = new Product { ProductId = "p1", IsDeleted = true, Category = categoryActive, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_inactive_cat",
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    CreatedAt = baseTime.AddMinutes(-10),
                    Product = new Product { ProductId = "p2", IsDeleted = false, Category = categoryInactive, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                // The following two are valid auctions
                new Auction
                {
                    AuctionId = "auc_valid_1",
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    CreatedAt = baseTime.AddMinutes(-15),
                    Product = new Product { ProductId = "p3", IsDeleted = false, Category = categoryActive, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_valid_2",
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    CreatedAt = baseTime.AddMinutes(-20),
                    Product = new Product { ProductId = "p4", IsDeleted = false, Category = categoryActive, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            // Two valid auctions in total (auc_valid_1 and auc_valid_2).
            // Ordered by default descending CreatedAt: auc_valid_1 (CreatedAt = -15m) then auc_valid_2 (CreatedAt = -20m).
            // Page = 2, PageSize = 1 should return auc_valid_2.
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetAuctionsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.TotalPages.Should().Be(2);
            result.Page.Should().Be(2);
            result.PageSize.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].AuctionId.Should().Be("auc_valid_2");
        }

        #endregion
    }
}
