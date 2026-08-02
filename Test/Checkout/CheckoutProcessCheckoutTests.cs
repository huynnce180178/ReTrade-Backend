using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Services.Checkout;
using RetradeBE.Services.Ghn;
using Xunit;

namespace Test.CheckoutTests
{
    public class CheckoutProcessCheckoutTests
    {
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<IGhnService> _ghnService;
        private readonly Mock<IHubContext<OrderHub>> _orderHub;
        private readonly CheckoutService _service;

        public CheckoutProcessCheckoutTests()
        {
            _context = new Mock<AppDbContext>();
            _ghnService = new Mock<IGhnService>();
            _orderHub = new Mock<IHubContext<OrderHub>>();

            _service = new CheckoutService(
                _context.Object,
                _ghnService.Object,
                _orderHub.Object
            );
        }

        private void SetupDefaultDbSetsAndSignalR()
        {
            _context.Setup(c => c.Wishlist).Returns(new List<Wishlist>().AsMockDbSet().Object);
            _context.Setup(c => c.WishlistItem).Returns(new List<WishlistItem>().AsMockDbSet().Object);
            _context.Setup(c => c.Voucher).Returns(new List<Voucher>().AsMockDbSet().Object);
            _context.Setup(c => c.MyVoucher).Returns(new List<MyVoucher>().AsMockDbSet().Object);

            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
            _orderHub.Setup(h => h.Clients).Returns(mockClients.Object);
        }

        #region Normal Tests (N)

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldCreateOrder_WhenDataIsValidWithoutVoucher()
        {
            // Arrange (UTCID01)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_001";
            string userId = "usr_001";
            string sellerId = "usr_seller";
            string productId = "prod_001";
            string addressId = "addr_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product
            {
                ProductId = productId,
                SellerId = sellerId,
                Price = 100000,
                StockQuantity = 5
            };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 20000 } });

            var mockOrderDbSet = new List<Order>().AsMockDbSet();
            _context.Setup(c => c.Order).Returns(mockOrderDbSet.Object);

            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var request = new CheckoutRequestDto
            {
                ProductId = productId,
                AddressId = addressId,
                Quantity = 1,
                PaymentMethod = "cod"
            };

            // Act
            var orderId = await _service.ProcessCheckoutAsync(request, accountId);

            // Assert
            orderId.Should().NotBeNullOrEmpty();
            _context.Verify(c => c.SaveChangesAsync(default), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldCreateOrder_WhenValidVoucherApplied()
        {
            // Arrange (UTCID02)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_001";
            string userId = "usr_001";
            string sellerId = "usr_seller";
            string productId = "prod_001";
            string addressId = "addr_001";
            string voucherCode = "DISCOUNT10";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product
            {
                ProductId = productId,
                SellerId = sellerId,
                Price = 100000,
                StockQuantity = 5
            };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            var voucher = new Voucher
            {
                VoucherId = "v_100",
                Code = voucherCode,
                Status = "Active",
                Quantity = 10,
                DiscountType = "Percentage",
                DiscountValue = 10
            };
            _context.Setup(c => c.Voucher).Returns(new List<Voucher> { voucher }.AsMockDbSet().Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 20000 } });

            var mockOrderDbSet = new List<Order>().AsMockDbSet();
            _context.Setup(c => c.Order).Returns(mockOrderDbSet.Object);

            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var request = new CheckoutRequestDto
            {
                ProductId = productId,
                AddressId = addressId,
                Quantity = 1,
                PaymentMethod = "cod",
                VoucherCode = voucherCode
            };

            // Act
            var orderId = await _service.ProcessCheckoutAsync(request, accountId);

            // Assert
            orderId.Should().NotBeNullOrEmpty();
            voucher.Quantity.Should().Be(9);
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldSetStatusToAwaitingPayment_WhenPaymentMethodIsVnpay()
        {
            // Arrange (UTCID03)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_vnpay";
            string userId = "usr_vnpay";
            string sellerId = "usr_seller";
            string productId = "prod_001";
            string addressId = "addr_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product { ProductId = productId, SellerId = sellerId, Price = 200000, StockQuantity = 2 };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 15000 } });

            var orders = new List<Order>();
            var mockOrderDbSet = orders.AsMockDbSet();
            mockOrderDbSet.Setup(m => m.Add(It.IsAny<Order>())).Callback<Order>(o => orders.Add(o));
            _context.Setup(c => c.Order).Returns(mockOrderDbSet.Object);

            var request = new CheckoutRequestDto { ProductId = productId, AddressId = addressId, Quantity = 1, PaymentMethod = "vnpay" };

            // Act
            var orderId = await _service.ProcessCheckoutAsync(request, accountId);

            // Assert
            orderId.Should().NotBeNullOrEmpty();
            orders.Should().ContainSingle();
            orders.First().Status.Should().Be("AwaitingPayment");
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldSetStatusToPending_WhenPaymentMethodIsCod()
        {
            // Arrange (UTCID04)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_cod";
            string userId = "usr_cod";
            string sellerId = "usr_seller";
            string productId = "prod_001";
            string addressId = "addr_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product { ProductId = productId, SellerId = sellerId, Price = 150000, StockQuantity = 3 };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 15000 } });

            var orders = new List<Order>();
            var mockOrderDbSet = orders.AsMockDbSet();
            mockOrderDbSet.Setup(m => m.Add(It.IsAny<Order>())).Callback<Order>(o => orders.Add(o));
            _context.Setup(c => c.Order).Returns(mockOrderDbSet.Object);

            var request = new CheckoutRequestDto { ProductId = productId, AddressId = addressId, Quantity = 1, PaymentMethod = "cod" };

            // Act
            var orderId = await _service.ProcessCheckoutAsync(request, accountId);

            // Assert
            orderId.Should().NotBeNullOrEmpty();
            orders.Should().ContainSingle();
            orders.First().Status.Should().Be("Pending");
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldRemoveItemFromWishlist_WhenProductIsInUserWishlist()
        {
            // Arrange (UTCID05)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_001";
            string userId = "usr_001";
            string sellerId = "usr_seller";
            string productId = "prod_wishlist";
            string addressId = "addr_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product { ProductId = productId, SellerId = sellerId, Price = 100000, StockQuantity = 5 };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            var wishlist = new Wishlist { WishlistId = "wl_001", UserId = userId, Status = "Active", IsDeleted = false };
            var wishlistItem = new WishlistItem { WishlistId = "wl_001", ProductId = productId };
            _context.Setup(c => c.Wishlist).Returns(new List<Wishlist> { wishlist }.AsMockDbSet().Object);

            var mockWishlistItemDbSet = new List<WishlistItem> { wishlistItem }.AsMockDbSet();
            _context.Setup(c => c.WishlistItem).Returns(mockWishlistItemDbSet.Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 10000 } });

            var mockOrderDbSet = new List<Order>().AsMockDbSet();
            _context.Setup(c => c.Order).Returns(mockOrderDbSet.Object);

            var request = new CheckoutRequestDto { ProductId = productId, AddressId = addressId, Quantity = 1 };

            // Act
            await _service.ProcessCheckoutAsync(request, accountId);

            // Assert
            mockWishlistItemDbSet.Verify(m => m.Remove(It.IsAny<WishlistItem>()), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldThrowException_WhenAccountNotFound()
        {
            // Arrange (UTCID06)
            string accountId = "non_existing_acc";
            _context.Setup(c => c.Account).Returns(new List<Account>().AsMockDbSet().Object);

            var request = new CheckoutRequestDto { ProductId = "prod_001", AddressId = "addr_001", Quantity = 1 };

            // Act & Assert
            var act = async () => await _service.ProcessCheckoutAsync(request, accountId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Account not found or not linked to a user.");
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldThrowException_WhenProductNotFound()
        {
            // Arrange (UTCID07)
            string accountId = "acc_001";
            var account = new Account { AccountId = accountId, UserId = "usr_001" };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product>().AsMockDbSet().Object);

            var request = new CheckoutRequestDto { ProductId = "non_existing_prod", AddressId = "addr_001", Quantity = 1 };

            // Act & Assert
            var act = async () => await _service.ProcessCheckoutAsync(request, accountId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Product not found");
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldThrowException_WhenAddressNotFound()
        {
            // Arrange (UTCID08)
            string accountId = "acc_001";
            string productId = "prod_001";

            var account = new Account { AccountId = accountId, UserId = "usr_001" };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product { ProductId = productId, StockQuantity = 10 };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            _context.Setup(c => c.Address).Returns(new List<Address>().AsMockDbSet().Object);

            var request = new CheckoutRequestDto { ProductId = productId, AddressId = "non_existing_addr", Quantity = 1 };

            // Act & Assert
            var act = async () => await _service.ProcessCheckoutAsync(request, accountId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Address not found");
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldThrowException_WhenStockIsNotEnough()
        {
            // Arrange (UTCID09)
            string accountId = "acc_001";
            string productId = "prod_001";
            string addressId = "addr_001";

            var account = new Account { AccountId = accountId, UserId = "usr_001" };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product { ProductId = productId, StockQuantity = 1 };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var address = new Address { AddressId = addressId };
            _context.Setup(c => c.Address).Returns(new List<Address> { address }.AsMockDbSet().Object);

            var request = new CheckoutRequestDto { ProductId = productId, AddressId = addressId, Quantity = 5 };

            // Act & Assert
            var act = async () => await _service.ProcessCheckoutAsync(request, accountId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Not enough stock");
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldThrowException_WhenVoucherIsInvalidOrInactive()
        {
            // Arrange (UTCID10)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_001";
            string userId = "usr_001";
            string productId = "prod_001";
            string addressId = "addr_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            string sellerId = "usr_seller";
            var product = new RetradeBE.Models.Product { ProductId = productId, SellerId = sellerId, StockQuantity = 10, Price = 50000 };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 10000 } });

            var request = new CheckoutRequestDto
            {
                ProductId = productId,
                AddressId = addressId,
                Quantity = 1,
                VoucherCode = "INVALID_CODE"
            };

            // Act & Assert
            var act = async () => await _service.ProcessCheckoutAsync(request, accountId);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Invalid or inactive voucher code.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldHandleBoundary_WhenQuantityEqualsStockQuantity()
        {
            // Arrange (UTCID11)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_001";
            string userId = "usr_001";
            string sellerId = "usr_seller";
            string productId = "prod_001";
            string addressId = "addr_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product
            {
                ProductId = productId,
                SellerId = sellerId,
                Price = 50000,
                StockQuantity = 2
            };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 15000 } });

            var mockOrderDbSet = new List<Order>().AsMockDbSet();
            _context.Setup(c => c.Order).Returns(mockOrderDbSet.Object);

            _context.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var request = new CheckoutRequestDto
            {
                ProductId = productId,
                AddressId = addressId,
                Quantity = 2,
                PaymentMethod = "cod"
            };

            // Act
            var orderId = await _service.ProcessCheckoutAsync(request, accountId);

            // Assert
            orderId.Should().NotBeNullOrEmpty();
            product.StockQuantity.Should().Be(0);
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldUpdateProductStatusToSold_WhenStockReachesZero()
        {
            // Arrange (UTCID12)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_001";
            string userId = "usr_001";
            string sellerId = "usr_seller";
            string productId = "prod_001";
            string addressId = "addr_001";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product { ProductId = productId, SellerId = sellerId, Price = 50000, StockQuantity = 1, Status = "Active" };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 10000 } });

            var mockOrderDbSet = new List<Order>().AsMockDbSet();
            _context.Setup(c => c.Order).Returns(mockOrderDbSet.Object);

            var request = new CheckoutRequestDto { ProductId = productId, AddressId = addressId, Quantity = 1 };

            // Act
            await _service.ProcessCheckoutAsync(request, accountId);

            // Assert
            product.StockQuantity.Should().Be(0);
            product.Status.Should().Be("Sold");
        }

        [Fact]
        public async Task ProcessCheckoutAsync_ShouldCalculatePercentageDiscount_WhenPercentageVoucherApplied()
        {
            // Arrange (UTCID13)
            SetupDefaultDbSetsAndSignalR();

            string accountId = "acc_001";
            string userId = "usr_001";
            string sellerId = "usr_seller";
            string productId = "prod_001";
            string addressId = "addr_001";
            string voucherCode = "20PERCENT";

            var account = new Account { AccountId = accountId, UserId = userId };
            _context.Setup(c => c.Account).Returns(new List<Account> { account }.AsMockDbSet().Object);

            var product = new RetradeBE.Models.Product { ProductId = productId, SellerId = sellerId, Price = 100000, StockQuantity = 5 };
            _context.Setup(c => c.Product).Returns(new List<RetradeBE.Models.Product> { product }.AsMockDbSet().Object);

            var buyerAddress = new Address { AddressId = addressId, UserId = userId, DistrictId = 1, WardCode = "101" };
            var sellerAddress = new Address { AddressId = "addr_seller", UserId = sellerId, DistrictId = 2, WardCode = "102", IsDefault = true };
            _context.Setup(c => c.Address).Returns(new List<Address> { buyerAddress, sellerAddress }.AsMockDbSet().Object);

            var voucher = new Voucher
            {
                VoucherId = "v_pct",
                Code = voucherCode,
                Status = "Active",
                Quantity = 5,
                DiscountType = "Percentage",
                DiscountValue = 20
            };
            _context.Setup(c => c.Voucher).Returns(new List<Voucher> { voucher }.AsMockDbSet().Object);

            _ghnService.Setup(g => g.CalculateFeeAsync(It.IsAny<GhnCalculateFeeRequest>()))
                .ReturnsAsync(new GhnCalculateFeeResponse { Code = 200, Data = new GhnFeeData { Total = 10000 } });

            var orders = new List<Order>();
            var mockOrderDbSet = orders.AsMockDbSet();
            mockOrderDbSet.Setup(m => m.Add(It.IsAny<Order>())).Callback<Order>(o => orders.Add(o));
            _context.Setup(c => c.Order).Returns(mockOrderDbSet.Object);

            var request = new CheckoutRequestDto { ProductId = productId, AddressId = addressId, Quantity = 1, VoucherCode = voucherCode };

            // Act
            await _service.ProcessCheckoutAsync(request, accountId);

            // Assert
            orders.First().DiscountAmount.Should().Be(20000);
            orders.First().FinalAmount.Should().Be(90000);
        }

        #endregion
    }
}
