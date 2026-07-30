using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using RetradeBE.Services;
using Xunit;

namespace Test.AdminProductTests
{
    public class AdminProductApproveProductTests
    {
        private readonly Mock<IAdminProductRepository> _adminProductRepository;
        private readonly Mock<AppDbContext> _context;
        private readonly Mock<INotificationService> _notificationService;
        private readonly AdminProductService _service;

        public AdminProductApproveProductTests()
        {
            _adminProductRepository = new Mock<IAdminProductRepository>();
            _context = new Mock<AppDbContext>();
            _notificationService = new Mock<INotificationService>();

            _service = new AdminProductService(
                _adminProductRepository.Object,
                _context.Object,
                _notificationService.Object
            );
        }

        #region Normal Tests (N)

        [Fact]
        public async Task ApproveProductAsync_ShouldApproveSaleProduct_WhenStatusIsPending()
        {
            // Arrange
            string productId = "prod_1";
            var product = new Product
            {
                ProductId = productId,
                Name = "Sale Book",
                Status = ProductStatusEnum.Pending.ToString(),
                SellerId = "seller_123",
                Category = new Category { Status = "Active" }
            };

            var dto = new AdminProductApprovalDto { IsApproved = true };

            _adminProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
            _adminProductRepository.Setup(x => x.UpdateAsync(product)).Returns(Task.CompletedTask);

            var mockNotifyDbSet = new List<Notification>().AsMockDbSet();
            _context.Setup(c => c.Notification).Returns(mockNotifyDbSet.Object);

            // Act
            var result = await _service.ApproveProductAsync(productId, dto);

            // Assert
            result.Should().BeTrue();
            product.Status.Should().Be(ProductStatusEnum.Accepted.ToString());

            _adminProductRepository.Verify(x => x.UpdateAsync(product), Times.Once);
            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task ApproveProductAsync_ShouldRejectSaleProduct_WhenStatusIsPendingAndReasonProvided()
        {
            // Arrange
            string productId = "prod_1";
            var product = new Product
            {
                ProductId = productId,
                Name = "Sale Book",
                Status = ProductStatusEnum.Pending.ToString(),
                SellerId = "seller_123"
            };

            var dto = new AdminProductApprovalDto { IsApproved = false, RejectReason = "Wrong Category" };

            _adminProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
            _adminProductRepository.Setup(x => x.UpdateAsync(product)).Returns(Task.CompletedTask);

            var mockNotifyDbSet = new List<Notification>().AsMockDbSet();
            _context.Setup(c => c.Notification).Returns(mockNotifyDbSet.Object);

            // Act
            var result = await _service.ApproveProductAsync(productId, dto);

            // Assert
            result.Should().BeTrue();
            product.Status.Should().Be(ProductStatusEnum.SaleRejected.ToString());

            _adminProductRepository.Verify(x => x.UpdateAsync(product), Times.Once);
            _context.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        #endregion

        #region Abnormal Tests (A)

        [Fact]
        public async Task ApproveProductAsync_ShouldThrowException_WhenProductDoesNotExist()
        {
            // Arrange
            string productId = "invalid_prod";
            _adminProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

            // Act
            Func<Task> act = async () => await _service.ApproveProductAsync(productId, new AdminProductApprovalDto());

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Product does not exist.");
        }

        [Fact]
        public async Task ApproveProductAsync_ShouldThrowException_WhenCategoryIsInactive()
        {
            // Arrange
            string productId = "prod_1";
            var product = new Product
            {
                ProductId = productId,
                Name = "Sale Book",
                Status = ProductStatusEnum.Pending.ToString(),
                Category = new Category { Status = "Inactive" } // Inactive category
            };

            var dto = new AdminProductApprovalDto { IsApproved = true };

            _adminProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

            // Act
            Func<Task> act = async () => await _service.ApproveProductAsync(productId, dto);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Cannot approve product because its category has not been approved yet.");
        }

        [Fact]
        public async Task ApproveProductAsync_ShouldThrowException_WhenRejectionReasonIsMissing()
        {
            // Arrange
            string productId = "prod_1";
            var product = new Product
            {
                ProductId = productId,
                Name = "Sale Book",
                Status = ProductStatusEnum.Pending.ToString()
            };

            var dto = new AdminProductApprovalDto { IsApproved = false, RejectReason = null }; // Missing reason

            _adminProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

            // Act
            Func<Task> act = async () => await _service.ApproveProductAsync(productId, dto);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Please provide a rejection reason.");
        }

        [Fact]
        public async Task ApproveProductAsync_ShouldThrowException_WhenStatusDoesNotSupportApproval()
        {
            // Arrange
            string productId = "prod_1";
            var product = new Product
            {
                ProductId = productId,
                Name = "Sale Book",
                Status = ProductStatusEnum.Accepted.ToString() // Already approved
            };

            var dto = new AdminProductApprovalDto { IsApproved = true };

            _adminProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

            // Act
            Func<Task> act = async () => await _service.ApproveProductAsync(productId, dto);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Current status 'Accepted' does not support approval.");
        }

        #endregion

        #region Boundary Tests (B)

        [Fact]
        public async Task ApproveProductAsync_ShouldApproveSuccessfully_WhenCategoryIsNull()
        {
            // Arrange
            string productId = "prod_1";
            var product = new Product
            {
                ProductId = productId,
                Name = "Sale Book",
                Status = ProductStatusEnum.Pending.ToString(),
                SellerId = "seller_123",
                Category = null // Null category
            };

            var dto = new AdminProductApprovalDto { IsApproved = true };

            _adminProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
            _adminProductRepository.Setup(x => x.UpdateAsync(product)).Returns(Task.CompletedTask);

            var mockNotifyDbSet = new List<Notification>().AsMockDbSet();
            _context.Setup(c => c.Notification).Returns(mockNotifyDbSet.Object);

            // Act
            var result = await _service.ApproveProductAsync(productId, dto);

            // Assert
            result.Should().BeTrue();
            product.Status.Should().Be(ProductStatusEnum.Accepted.ToString());
        }

        #endregion
    }
}
