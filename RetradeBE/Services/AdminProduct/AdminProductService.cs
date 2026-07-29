using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetradeBE.Services
{
    public class AdminProductService : IAdminProductService
    {
        private readonly IAdminProductRepository _repository;
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminProductService(
            IAdminProductRepository repository,
            AppDbContext context,
            INotificationService notificationService)
        {
            _repository = repository;
            _context = context;
            _notificationService = notificationService;
        }

        public IQueryable<ProductListDto> Query()
        {
            return _repository.Query()
                .Select(p => new ProductListDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Status = p.Status,
                    Condition = p.Condition,
                    CreatedAt = p.CreatedAt,
                    SellerId = p.SellerId,
                    SellerName = p.Seller != null ? (p.Seller.FirstName + " " + p.Seller.LastName).Trim() : null,
                    MainImageUrl = p.ProductImage.Where(pi => pi.IsMain == true).Select(pi => pi.Image.ImageUrl).FirstOrDefault()
                                   ?? p.ProductImage.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.ImageUrl).FirstOrDefault(),
                    IsDeleted = p.IsDeleted
                });
        }

        public async Task<PagedResultDto<ProductListDto>> GetProductsForApprovalAsync(ProductSearchQueryDto query)
        {
            var queryable = _repository.Query();

            // Default to showing Pending and Waiting products for approval
            if (string.IsNullOrEmpty(query.Status))
            {
                queryable = queryable.Where(p => p.Status == ProductStatusEnum.Pending.ToString() || p.Status == ProductStatusEnum.Waiting.ToString());
            }
            else
            {
                queryable = queryable.Where(p => p.Status == query.Status);
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var search = query.SearchTerm.ToLower();
                queryable = queryable.Where(p => p.Name.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(query.CategoryId))
            {
                queryable = queryable.Where(p => p.CategoryId == query.CategoryId);
            }

            int totalItems = await queryable.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / query.PageSize);
            if (totalPages == 0) totalPages = 1;

            var items = await queryable
                .OrderByDescending(p => p.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new ProductListDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Status = p.Status,
                    Condition = p.Condition,
                    CreatedAt = p.CreatedAt,
                    SellerId = p.SellerId,
                    SellerName = p.Seller != null ? (p.Seller.FirstName + " " + p.Seller.LastName).Trim() : null,
                    MainImageUrl = p.ProductImage.Where(pi => pi.IsMain == true).Select(pi => pi.Image.ImageUrl).FirstOrDefault()
                                   ?? p.ProductImage.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.ImageUrl).FirstOrDefault(),
                    IsDeleted = p.IsDeleted
                })
                .ToListAsync();

            return new PagedResultDto<ProductListDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = totalPages
            };
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(string productId)
        {
            var product = await _repository.GetByIdAsync(productId);
            if (product == null) return null;

            var sellerName = product.Seller != null ? $"{product.Seller.FirstName} {product.Seller.LastName}".Trim() : null;
            if (string.IsNullOrEmpty(sellerName) && product.Seller != null)
                sellerName = product.Seller.Email;

            return new ProductResponseDto
            {
                ProductId = product.ProductId,
                SellerId = product.SellerId,
                SellerName = sellerName,
                CategoryId = product.CategoryId,
                CategoryName = product.Category != null ? product.Category.Name : null,
                Name = product.Name,
                Description = product.Description,
                Condition = product.Condition,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                WeightGram = product.WeightGram,
                LengthCm = product.LengthCm,
                WidthCm = product.WidthCm,
                HeightCm = product.HeightCm,
                Status = product.Status,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                IsDeleted = product.IsDeleted,
                Images = product.ProductImage.Select(pi => new ProductImageDto
                {
                    ImageId = pi.ImageId,
                    ImageUrl = pi.Image?.ImageUrl,
                    AltText = pi.Image?.AltText,
                    IsMain = pi.IsMain,
                    SortOrder = pi.SortOrder
                }).OrderBy(i => i.SortOrder).ToList(),
                Attributes = product.ProductAttribute.Where(pa => pa.IsDeleted != true).Select(pa => new ProductAttributeValueDto
                {
                    AttributeId = pa.AttributeId,
                    AttributeName = pa.Attribute?.Name,
                    Value = pa.Value,
                    DataType = pa.Attribute?.DataType,
                    Unit = pa.Attribute?.Unit
                }).ToList()
            };
        }

        public async Task<bool> ApproveProductAsync(string productId, AdminProductApprovalDto dto)
        {
            var product = await _repository.GetByIdAsync(productId);
            if (product == null)
                throw new Exception("Product does not exist.");

            var currentStatus = product.Status;
            string newStatus;

            if (dto.IsApproved)
            {
                if (product.Category != null && product.Category.Status != "Active")
                {
                    throw new Exception("Cannot approve product because its category has not been approved yet.");
                }

                if (currentStatus == ProductStatusEnum.Pending.ToString())
                {
                    newStatus = ProductStatusEnum.Accepted.ToString();
                }
                else if (currentStatus == ProductStatusEnum.Waiting.ToString())
                {
                    newStatus = ProductStatusEnum.Ready.ToString();
                }
                else
                {
                    throw new Exception($"Current status '{currentStatus}' does not support approval.");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(dto.RejectReason))
                    throw new Exception("Please provide a rejection reason.");

                if (currentStatus == ProductStatusEnum.Pending.ToString())
                {
                    newStatus = ProductStatusEnum.SaleRejected.ToString();
                }
                else if (currentStatus == ProductStatusEnum.Waiting.ToString())
                {
                    newStatus = ProductStatusEnum.AuctionRejected.ToString();
                }
                else
                {
                    throw new Exception($"Current status '{currentStatus}' does not support rejection.");
                }
            }

            product.Status = newStatus;
            product.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(product);

            try
            {
                await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                {
                    UserId = product.SellerId,
                    Title = dto.IsApproved ? "Product Approved" : "Product Rejected",
                    Message = dto.IsApproved
                        ? $"Your product '{product.Name}' has been approved and is ready to be displayed on the platform."
                        : $"Your product '{product.Name}' has been rejected. Reason: {dto.RejectReason}",
                    Type = nameof(NotificationTypeEnum.System),
                    ReferenceId = product.ProductId
                });
            }
            catch { }

            return true;
        }
    }
}
