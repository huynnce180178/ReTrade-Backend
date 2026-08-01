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
    public class AuctionGetMyAuctionsTests
    {
        private readonly Mock<IAuctionRepository> _auctionRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IHubContext<AuctionHub>> _auctionHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly AuctionService _service;

        public AuctionGetMyAuctionsTests()
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
        public async Task GetMyAuctionsAsync_ShouldReturnPagedAuctions_WhenQueryIsEmpty()
        {
            // Arrange
            var accountId = "acc_123";
            var userId = "user_123";
            var query = new AuctionQueryDto();
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };
            
            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_1",
                    ProductId = "prod_1",
                    SellerId = userId,
                    StartingPrice = 100,
                    CurrentPrice = 100,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(2),
                    Status = "Ongoing",
                    CreatedAt = baseTime.AddDays(-1),
                    Product = new Product
                    {
                        ProductId = "prod_1",
                        Name = "My Product",
                        Category = category,
                        ProductImage = new List<ProductImage>()
                    },
                    Bid = new List<Bid>()
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyAuctionsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].AuctionId.Should().Be("auc_1");
            result.Items[0].ProductName.Should().Be("My Product");
        }

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldFilterBySearchTerm_WhenSearchTermIsProvided()
        {
            // Arrange
            var accountId = "acc_123";
            var userId = "user_123";
            var query = new AuctionQueryDto { SearchTerm = "special" };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_match",
                    ProductId = "prod_1",
                    SellerId = userId,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(2),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "prod_1", Name = "Special Item", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_no_match",
                    ProductId = "prod_2",
                    SellerId = userId,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(2),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "prod_2", Name = "Regular Item", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyAuctionsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items[0].AuctionId.Should().Be("auc_match");
        }

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldFilterByStatusUpcomingAndSortByStartingSoon_WhenStatusUpcomingIsRequested()
        {
            // Arrange
            var accountId = "acc_123";
            var userId = "user_123";
            var query = new AuctionQueryDto { Status = "Upcoming", SortBy = "starting_soon" };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_later",
                    SellerId = userId,
                    StartTime = baseTime.AddHours(3),
                    EndTime = baseTime.AddHours(6),
                    Status = "Upcoming",
                    Product = new Product { ProductId = "p1", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_soon",
                    SellerId = userId,
                    StartTime = baseTime.AddHours(1),
                    EndTime = baseTime.AddHours(5),
                    Status = "Upcoming",
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyAuctionsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items[0].AuctionId.Should().Be("auc_soon");
            result.Items[1].AuctionId.Should().Be("auc_later");
        }

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldFilterByStatusOngoingAndSortByPriceAsc_WhenStatusOngoingIsRequested()
        {
            // Arrange
            var accountId = "acc_123";
            var userId = "user_123";
            var query = new AuctionQueryDto { Status = "Ongoing", SortBy = "price_asc" };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_expensive",
                    SellerId = userId,
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
                    SellerId = userId,
                    StartingPrice = 100,
                    CurrentPrice = 100,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyAuctionsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items[0].AuctionId.Should().Be("auc_cheap");
            result.Items[1].AuctionId.Should().Be("auc_expensive");
        }

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldFilterByStatusEndedAndSortByEndingSoon_WhenStatusEndedIsRequested()
        {
            // Arrange
            var accountId = "acc_123";
            var userId = "user_123";
            var query = new AuctionQueryDto { Status = "Ended", SortBy = "ending_soon" };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_ended_long_ago",
                    SellerId = userId,
                    StartTime = baseTime.AddHours(-5),
                    EndTime = baseTime.AddHours(-3),
                    Status = "Ended",
                    Product = new Product { ProductId = "p1", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_ended_recently",
                    SellerId = userId,
                    StartTime = baseTime.AddHours(-5),
                    EndTime = baseTime.AddHours(-1),
                    Status = "Ended",
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyAuctionsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items[0].AuctionId.Should().Be("auc_ended_long_ago");
            result.Items[1].AuctionId.Should().Be("auc_ended_recently");
        }

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldFilterByCustomStatusAndSortByOldest_WhenCustomStatusAndOldestAreRequested()
        {
            // Arrange
            var accountId = "acc_123";
            var userId = "user_123";
            var query = new AuctionQueryDto { Status = "Cancelled", SortBy = "oldest" };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_new",
                    SellerId = userId,
                    Status = "Cancelled",
                    CreatedAt = baseTime.AddMinutes(-5),
                    Product = new Product { ProductId = "p1", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_old",
                    SellerId = userId,
                    Status = "Cancelled",
                    CreatedAt = baseTime.AddMinutes(-20),
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyAuctionsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2);
            result.Items[0].AuctionId.Should().Be("auc_old");
            result.Items[1].AuctionId.Should().Be("auc_new");
        }

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldSortByPriceDesc_WhenSortByPriceDescIsRequested()
        {
            // Arrange
            var accountId = "acc_123";
            var userId = "user_123";
            var query = new AuctionQueryDto { SortBy = "price_desc" };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var category = new Category { CategoryId = "cat_1", Status = "Active" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_cheap",
                    SellerId = userId,
                    StartingPrice = 100,
                    CurrentPrice = 100,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "p1", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_expensive",
                    SellerId = userId,
                    StartingPrice = 500,
                    CurrentPrice = 500,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    Product = new Product { ProductId = "p2", Category = category, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyAuctionsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.Items[0].AuctionId.Should().Be("auc_expensive");
            result.Items[1].AuctionId.Should().Be("auc_cheap");
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldThrowException_WhenAccountDoesNotExist()
        {
            // Arrange
            var accountId = "non_existent_acc";
            var query = new AuctionQueryDto();
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync((Account)null!);

            // Act & Assert
            await _service.Invoking(s => s.GetMyAuctionsAsync(accountId, query))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Account does not exist.");
        }

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldThrowException_WhenAccountNotLinkedToUser()
        {
            // Arrange
            var accountId = "acc_123";
            var query = new AuctionQueryDto();
            var account = new Account { AccountId = accountId, UserId = null };
            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

            // Act & Assert
            await _service.Invoking(s => s.GetMyAuctionsAsync(accountId, query))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Account is not linked to a user.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetMyAuctionsAsync_ShouldHandlePaginationAndExclusions_WhenPagingRequestedAndDeletedProductsExist()
        {
            // Arrange
            var accountId = "acc_123";
            var userId = "user_123";
            var query = new AuctionQueryDto { Page = 2, PageSize = 1 };
            var baseTime = DateTime.UtcNow.AddHours(7);

            var account = new Account { AccountId = accountId, UserId = userId };
            var categoryActive = new Category { CategoryId = "cat_active", Status = "Active" };
            var categoryInactive = new Category { CategoryId = "cat_inactive", Status = "Inactive" };

            var auctions = new List<Auction>
            {
                new Auction
                {
                    AuctionId = "auc_deleted_prod",
                    SellerId = userId,
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
                    SellerId = userId,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    CreatedAt = baseTime.AddMinutes(-10),
                    Product = new Product { ProductId = "p2", IsDeleted = false, Category = categoryInactive, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                },
                new Auction
                {
                    AuctionId = "auc_valid_1",
                    SellerId = userId,
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
                    SellerId = userId,
                    StartTime = baseTime.AddHours(-1),
                    EndTime = baseTime.AddHours(3),
                    Status = "Ongoing",
                    CreatedAt = baseTime.AddMinutes(-20),
                    Product = new Product { ProductId = "p4", IsDeleted = false, Category = categoryActive, ProductImage = new List<ProductImage>() },
                    Bid = new List<Bid>()
                }
            };

            _accountRepository.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
            _auctionRepository.Setup(x => x.Query()).Returns(auctions.AsAsyncQueryable());

            // Act
            var result = await _service.GetMyAuctionsAsync(accountId, query);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(2); // Only valid 1 and 2 are counted
            result.TotalPages.Should().Be(2);
            result.Page.Should().Be(2);
            result.Items.Should().HaveCount(1);
            result.Items[0].AuctionId.Should().Be("auc_valid_2"); // Paged element
        }

        #endregion
    }
}
