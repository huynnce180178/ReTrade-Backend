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

        public AdminProductService(
            IAdminProductRepository repository,
            AppDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public IQueryable<ProductListDto> Query()
        {
            return _repository.Query()
                .Where(p => p.IsDeleted != true)
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
                                   ?? p.ProductImage.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.ImageUrl).FirstOrDefault()
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
                                   ?? p.ProductImage.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.ImageUrl).FirstOrDefault()
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
                throw new Exception("Sản phẩm không tồn tại.");

            var currentStatus = product.Status;
            string newStatus;

            if (dto.IsApproved)
            {
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
                    throw new Exception($"Trạng thái hiện tại '{currentStatus}' không hỗ trợ duyệt.");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(dto.RejectReason))
                    throw new Exception("Vui lòng cung cấp lý do từ chối.");

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
                    throw new Exception($"Trạng thái hiện tại '{currentStatus}' không hỗ trợ từ chối duyệt.");
                }
            }

            product.Status = newStatus;
            product.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(product);

            // Create notification for Seller
            var notification = new Notification
            {
                NotificationId = $"NT{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                UserId = product.SellerId,
                Title = dto.IsApproved ? "Sản phẩm được phê duyệt" : "Sản phẩm bị từ chối duyệt",
                Message = dto.IsApproved
                    ? $"Sản phẩm '{product.Name}' của bạn đã được phê duyệt và sẵn sàng hiển thị trên nền tảng."
                    : $"Sản phẩm '{product.Name}' của bạn đã bị từ chối duyệt. Lý do: {dto.RejectReason}",
                Type = "ProductApproval",
                ReferenceId = product.ProductId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Notification.AddAsync(notification);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
