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
    public class AuctionGetAuctionByIdTests
    {
        private readonly Mock<IAuctionRepository> _auctionRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IHubContext<AuctionHub>> _auctionHub;
        private readonly Mock<INotificationService> _notificationService;
        private readonly IMapper _mapper;
        private readonly AuctionService _service;

        public AuctionGetAuctionByIdTests()
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
        public async Task GetAuctionByIdAsync_ShouldReturnAuctionDetail_WhenAuctionExistsWithNoBidsNoImagesNoAttributes()
        {
            // Arrange
            var auctionId = "auc_123";
            var category = new Category { CategoryId = "cat_1", Name = "Electronics" };
            var seller = new User { UserId = "seller_1", FirstName = "John", LastName = "Doe" };
            
            var auction = new Auction
            {
                AuctionId = auctionId,
                ProductId = "prod_1",
                SellerId = "seller_1",
                StartingPrice = 100,
                CurrentPrice = 100,
                Status = "Ongoing",
                Product = new Product
                {
                    ProductId = "prod_1",
                    Name = "Product 1",
                    Description = "Description 1",
                    Condition = "New",
                    StockQuantity = 1,
                    WeightGram = 1000,
                    LengthCm = 10,
                    WidthCm = 15,
                    HeightCm = 5,
                    Category = category,
                    ProductImage = new List<ProductImage>(),
                    ProductAttribute = new List<ProductAttribute>()
                },
                Seller = seller,
                Bid = new List<Bid>()
            };

            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().NotBeNull();
            result!.AuctionId.Should().Be(auctionId);
            result.ProductName.Should().Be("Product 1");
            result.ProductDescription.Should().Be("Description 1");
            result.CategoryName.Should().Be("Electronics");
            result.SellerName.Should().Be("John Doe");
            result.Images.Should().BeEmpty();
            result.Attributes.Should().BeEmpty();
            result.RecentBids.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldReturnAuctionDetailWithWinner_WhenWinnerIsSet()
        {
            // Arrange
            var auctionId = "auc_123";
            var winner = new User { UserId = "winner_1", FirstName = "Winner", LastName = "One" };
            var auction = new Auction
            {
                AuctionId = auctionId,
                WinnerId = "winner_1",
                Winner = winner,
                Bid = new List<Bid>()
            };

            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().NotBeNull();
            result!.WinnerId.Should().Be("winner_1");
            result.WinnerName.Should().Be("Winner One");
        }

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldReturnAuctionDetailWithImagesSortedBySortOrder_WhenProductImagesExist()
        {
            // Arrange
            var auctionId = "auc_123";
            
            var img1 = new Image { ImageId = "img_1", ImageUrl = "url_1", AltText = "alt_1" };
            var img2 = new Image { ImageId = "img_2", ImageUrl = "url_2", AltText = "alt_2" };

            var auction = new Auction
            {
                AuctionId = auctionId,
                Product = new Product
                {
                    ProductId = "prod_1",
                    ProductImage = new List<ProductImage>
                    {
                        new ProductImage { ImageId = "img_2", Image = img2, IsMain = false, SortOrder = 2 },
                        new ProductImage { ImageId = "img_1", Image = img1, IsMain = true, SortOrder = 1 }
                    }
                },
                Bid = new List<Bid>()
            };

            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().NotBeNull();
            result!.Images.Should().HaveCount(2);
            result.Images[0].ImageId.Should().Be("img_1"); // Sorted by SortOrder asc
            result.Images[0].IsMain.Should().BeTrue();
            result.Images[1].ImageId.Should().Be("img_2");
        }

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldReturnAuctionDetailWithActiveAttributes_WhenProductAttributesExist()
        {
            // Arrange
            var auctionId = "auc_123";
            var attr1 = new Attributes { AttributeId = "attr_1", Name = "Color", DataType = "String", Unit = "N/A" };
            var attr2 = new Attributes { AttributeId = "attr_2", Name = "Size", DataType = "String", Unit = "N/A" };

            var auction = new Auction
            {
                AuctionId = auctionId,
                Product = new Product
                {
                    ProductId = "prod_1",
                    ProductAttribute = new List<ProductAttribute>
                    {
                        new ProductAttribute { AttributeId = "attr_1", Attribute = attr1, Value = "Red", IsDeleted = false },
                        new ProductAttribute { AttributeId = "attr_2", Attribute = attr2, Value = "Large", IsDeleted = true } // Deleted attribute
                    }
                },
                Bid = new List<Bid>()
            };

            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().NotBeNull();
            result!.Attributes.Should().HaveCount(1);
            result.Attributes[0].AttributeId.Should().Be("attr_1");
            result.Attributes[0].AttributeName.Should().Be("Color");
            result.Attributes[0].Value.Should().Be("Red");
        }

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldReturnAuctionDetailWithRecentBidsSortedByCreatedAt_WhenBidsExist()
        {
            // Arrange
            var auctionId = "auc_123";
            var baseTime = DateTime.UtcNow;

            var bids = new List<Bid>();
            for (int i = 1; i <= 10; i++)
            {
                bids.Add(new Bid
                {
                    BidId = $"bid_{i}",
                    UserId = $"user_{i}",
                    BidAmount = 100 + i,
                    Status = "Active",
                    CreatedAt = baseTime.AddMinutes(i),
                    User = new User { UserId = $"user_{i}", FirstName = $"User{i}", LastName = "" }
                });
            }

            var auction = new Auction
            {
                AuctionId = auctionId,
                Bid = bids
            };

            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().NotBeNull();
            result!.RecentBids.Should().HaveCount(8); // Capped at 8 bids
            result.RecentBids[0].BidId.Should().Be("bid_10"); // Sorted by CreatedAt desc
            result.RecentBids[0].BidderName.Should().Be("User10");
        }

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldMapHighestBidAndCurrentPriceCorrectly_WhenBidsWithValuesExist()
        {
            // Arrange
            var auctionId = "auc_123";
            var auction = new Auction
            {
                AuctionId = auctionId,
                StartingPrice = 100,
                CurrentPrice = 100,
                Bid = new List<Bid>
                {
                    new Bid { BidId = "b1", BidAmount = 150 },
                    new Bid { BidId = "b2", BidAmount = 200 }
                }
            };

            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().NotBeNull();
            result!.CurrentPrice.Should().Be(200); // Current price becomes the highest bid
            result.HighestBid.Should().Be(200);
        }

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldReturnAuctionDetail_WhenProductIsNull()
        {
            // Arrange
            var auctionId = "auc_123";
            var auction = new Auction
            {
                AuctionId = auctionId,
                Product = null,
                Bid = new List<Bid>()
            };

            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().NotBeNull();
            result!.ProductName.Should().BeNull();
            result.ProductDescription.Should().BeNull();
            result.Images.Should().BeEmpty();
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldReturnNull_WhenAuctionDoesNotExist()
        {
            // Arrange
            var auctionId = "non_existent";
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync((Auction)null!);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldThrowException_WhenDatabaseThrowsException()
        {
            // Arrange
            var auctionId = "auc_123";
            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).Throws(new Exception("DB connection timeout"));

            // Act & Assert
            await _service.Invoking(s => s.GetAuctionByIdAsync(auctionId))
                .Should().ThrowAsync<Exception>()
                .WithMessage("DB connection timeout");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task GetAuctionByIdAsync_ShouldReturnNull_WhenProductIsDeleted()
        {
            // Arrange
            var auctionId = "auc_123";
            var auction = new Auction
            {
                AuctionId = auctionId,
                Product = new Product
                {
                    ProductId = "prod_1",
                    IsDeleted = true
                }
            };

            _auctionRepository.Setup(x => x.GetByIdAsync(auctionId)).ReturnsAsync(auction);

            // Act
            var result = await _service.GetAuctionByIdAsync(auctionId);

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
