using AutoMapper;
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
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IAccountRepository _accountRepository;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public ProductService(
            IProductRepository repository,
            IAccountRepository accountRepository,
            AppDbContext context,
            IMapper mapper,
            INotificationService notificationService)
        {
            _repository = repository;
            _accountRepository = accountRepository;
            _context = context;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<ProductResponseDto> CreateProductAsync(string accountId, ProductCreateDto dto)
        {
            // 1. Map accountId to UserId
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Account does not exist.");

            var userId = account.UserId;
            if (string.IsNullOrEmpty(userId))
                throw new Exception("Account is not linked to user information.");

            // 2. Validate Category
            var category = await _context.Category
                .Include(c => c.Attributes)
                .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);
            if (category == null)
                throw new Exception("Category does not exist.");

            // 3. Generate ProductId
            var productId = await GenerateProductIdAsync();

            // 4. Determine initial status and handle auction constraints
            string initialStatus;
            decimal? finalPrice = dto.Price;
            int? finalStock = dto.StockQuantity;

            if (dto.IsForAuction)
            {
                initialStatus = ProductStatusEnum.Waiting.ToString();
                finalPrice = null; // Auction price is managed in the Auction table
                finalStock = 1;    // Auction product quantity is always 1
            }
            else
            {
                initialStatus = ProductStatusEnum.Pending.ToString();
                if (finalPrice <= 0)
                    throw new Exception("Product price must be greater than 0.");
                if (finalStock <= 0)
                    throw new Exception("Product stock quantity must be greater than 0.");
            }

            if (!string.IsNullOrEmpty(dto.Condition))
            {
                var validConditions = new[] { 
                    ProductConditionEnum.New.ToString(), 
                    ProductConditionEnum.LikeNew.ToString(), 
                    ProductConditionEnum.Excellent.ToString(), 
                    ProductConditionEnum.Good.ToString(), 
                    ProductConditionEnum.Fair.ToString(), 
                    ProductConditionEnum.Used.ToString(), 
                    ProductConditionEnum.Damaged.ToString(), 
                    ProductConditionEnum.ForParts.ToString() 
                };
                if (!validConditions.Contains(dto.Condition!))
                {
                    throw new Exception("Invalid product condition.");
                }
            }

            var product = new Product
            {
                ProductId = productId,
                SellerId = userId,
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                Condition = dto.Condition,
                Price = finalPrice,
                StockQuantity = finalStock,
                WeightGram = dto.WeightGram,
                LengthCm = dto.LengthCm,
                WidthCm = dto.WidthCm,
                HeightCm = dto.HeightCm,
                Status = initialStatus,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 5. Handle Images (exactly one is main)
            if (dto.Images != null && dto.Images.Any())
            {
                // Ensure at least one is main. If none, set first to main.
                var hasMain = dto.Images.Any(i => i.IsMain == true);
                var firstImage = dto.Images.First();

                int imgIndex = 1;
                foreach (var imgDto in dto.Images)
                {
                    var imageId = await GenerateImageIdAsync();
                    var isMainImage = hasMain ? (imgDto.IsMain ?? false) : (imgDto == firstImage);

                    var image = new Image
                    {
                        ImageId = imageId,
                        ImageUrl = imgDto.ImageUrl,
                        AltText = imgDto.AltText ?? dto.Name,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Image.AddAsync(image);

                    var productImage = new ProductImage
                    {
                        ProductId = productId,
                        ImageId = imageId,
                        IsMain = isMainImage,
                        SortOrder = imgDto.SortOrder ?? imgIndex,
                        CreatedAt = DateTime.UtcNow
                    };
                    product.ProductImage.Add(productImage);
                    imgIndex++;
                }
            }
            else
            {
                throw new Exception("Product must have at least one image.");
            }

            // 6. Handle Dynamic Attributes
            if (dto.Attributes != null)
            {
                ValidateProductAttributes(category, dto.Attributes);

                foreach (var attrValDto in dto.Attributes)
                {
                    var categoryAttr = category.Attributes.FirstOrDefault(a => a.AttributeId == attrValDto.AttributeId && a.IsDeleted != true);
                    if (categoryAttr == null)
                        continue;

                    var productAttrId = await GenerateProductAttributeIdAsync();
                    var productAttr = new ProductAttribute
                    {
                        ProductAttributeId = productAttrId,
                        ProductId = productId,
                        AttributeId = attrValDto.AttributeId,
                        Value = attrValDto.Value,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    product.ProductAttribute.Add(productAttr);
                }
            }

            await _repository.AddAsync(product);

            // Send notification to Admins
            await _notificationService.NotifyAdminsAsync(
                "New Product Needs Approval",
                $"A new product '{product.Name}' is pending your approval.",
                nameof(NotificationTypeEnum.System),
                productId
            );

            // Re-fetch to return fully populated object
            var savedProduct = await _repository.GetByIdAsync(productId);
            return MapToResponseDto(savedProduct!);
        }

        public async Task<ProductResponseDto> UpdateProductAsync(string productId, string accountId, ProductUpdateDto dto)
        {
            // 1. Map accountId to UserId
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Account does not exist.");

            var userId = account.UserId;
            if (string.IsNullOrEmpty(userId))
                throw new Exception("Account is not linked to user information.");

            // 2. Fetch existing Product
            var product = await _context.Product
                .Include(p => p.Category)
                    .ThenInclude(c => c.Attributes)
                .Include(p => p.ProductImage)
                    .ThenInclude(pi => pi.Image)
                .Include(p => p.ProductAttribute)
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsDeleted != true);

            if (product == null)
                throw new Exception("Product does not exist.");

            if (product.SellerId != userId)
                throw new Exception("You do not have permission to edit this product.");

            // 3. Update standard fields
            product.Name = dto.Name;
            product.Description = dto.Description;
            if (!string.IsNullOrEmpty(dto.Condition))
            {
                var validConditions = new[] { 
                    ProductConditionEnum.New.ToString(), 
                    ProductConditionEnum.LikeNew.ToString(), 
                    ProductConditionEnum.Excellent.ToString(), 
                    ProductConditionEnum.Good.ToString(), 
                    ProductConditionEnum.Fair.ToString(), 
                    ProductConditionEnum.Used.ToString(), 
                    ProductConditionEnum.Damaged.ToString(), 
                    ProductConditionEnum.ForParts.ToString() 
                };
                if (!validConditions.Contains(dto.Condition!))
                {
                    throw new Exception("Invalid product condition.");
                }
            }

            product.Condition = dto.Condition;
            product.WeightGram = dto.WeightGram;
            product.LengthCm = dto.LengthCm;
            product.WidthCm = dto.WidthCm;
            product.HeightCm = dto.HeightCm;
            product.UpdatedAt = DateTime.UtcNow;

            // Handle auction status checking
            bool isAuction = product.Status == ProductStatusEnum.Waiting.ToString() ||
                             product.Status == ProductStatusEnum.Ready.ToString() ||
                             product.Status == ProductStatusEnum.AuctionRejected.ToString();

            if (isAuction)
            {
                product.Price = null;
                product.StockQuantity = 1;
                product.Status = ProductStatusEnum.Waiting.ToString(); // Revert to waiting for admin approval
            }
            else
            {
                if (dto.Price <= 0)
                    throw new Exception("Product price must be greater than 0.");
                if (dto.StockQuantity <= 0)
                    throw new Exception("Product stock quantity must be greater than 0.");

                product.Price = dto.Price;
                product.StockQuantity = dto.StockQuantity;
                product.Status = ProductStatusEnum.Pending.ToString(); // Revert to pending admin approval
            }

            // 4. Synchronize Images
            if (dto.Images == null || !dto.Images.Any())
                throw new Exception("Product must have at least one image.");

            // Differential Image synchronization to avoid EF Core tracking conflicts
            var incomingImages = dto.Images;
            var existingProductImages = product.ProductImage.ToList();

            var hasMain = incomingImages.Any(i => i.IsMain == true);
            var firstImage = incomingImages.First();

            // Step 4.1: Remove ProductImage and Image entries that are no longer present
            var incomingImageIds = incomingImages.Where(i => !string.IsNullOrEmpty(i.ImageId)).Select(i => i.ImageId).ToHashSet();
            foreach (var existingPI in existingProductImages)
            {
                if (!incomingImageIds.Contains(existingPI.ImageId))
                {
                    _context.ProductImage.Remove(existingPI);
                    var img = existingPI.Image;
                    if (img != null)
                    {
                        _context.Image.Remove(img);
                    }
                }
            }

            // Step 4.2: Update existing links and insert new ones
            int imgIndex = 1;
            foreach (var imgDto in incomingImages)
            {
                var isMainImage = hasMain ? (imgDto.IsMain ?? false) : (imgDto == firstImage);

                if (!string.IsNullOrEmpty(imgDto.ImageId))
                {
                    var existingPI = existingProductImages.FirstOrDefault(pi => pi.ImageId == imgDto.ImageId);
                    if (existingPI != null)
                    {
                        existingPI.IsMain = isMainImage;
                        existingPI.SortOrder = imgDto.SortOrder ?? imgIndex;

                        var img = existingPI.Image;
                        if (img != null)
                        {
                            img.ImageUrl = imgDto.ImageUrl ?? img.ImageUrl;
                            img.AltText = imgDto.AltText ?? product.Name;
                        }
                    }
                }
                else
                {
                    var newImageId = await GenerateImageIdAsync();
                    var newImg = new Image
                    {
                        ImageId = newImageId,
                        ImageUrl = imgDto.ImageUrl,
                        AltText = imgDto.AltText ?? product.Name,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Image.AddAsync(newImg);

                    var newProductImage = new ProductImage
                    {
                        ProductId = productId,
                        ImageId = newImageId,
                        IsMain = isMainImage,
                        SortOrder = imgDto.SortOrder ?? imgIndex,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ProductImage.Add(newProductImage);
                }
                imgIndex++;
            }

            // 5. Synchronize Dynamic Attributes
            var category = product.Category;
            if (category != null && dto.Attributes != null)
            {
                ValidateProductAttributes(category, dto.Attributes);

                // Soft delete existing attributes
                foreach (var existingAttr in product.ProductAttribute)
                {
                    existingAttr.IsDeleted = true;
                    existingAttr.UpdatedAt = DateTime.UtcNow;
                }

                foreach (var attrValDto in dto.Attributes)
                {
                    var categoryAttr = category.Attributes.FirstOrDefault(a => a.AttributeId == attrValDto.AttributeId && a.IsDeleted != true);
                    if (categoryAttr == null)
                        continue;

                    var existing = product.ProductAttribute.FirstOrDefault(pa => pa.AttributeId == attrValDto.AttributeId);
                    if (existing != null)
                    {
                        existing.Value = attrValDto.Value;
                        existing.IsDeleted = false;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var productAttrId = await GenerateProductAttributeIdAsync();
                        var productAttr = new ProductAttribute
                        {
                            ProductAttributeId = productAttrId,
                            ProductId = productId,
                            AttributeId = attrValDto.AttributeId,
                            Value = attrValDto.Value,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        product.ProductAttribute.Add(productAttr);
                    }
                }
            }

            await _repository.UpdateAsync(product);

            var updatedProduct = await _repository.GetByIdAsync(productId);
            return MapToResponseDto(updatedProduct!);
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(string productId)
        {
            var product = await _repository.GetByIdAsync(productId);
            if (product == null) return null;
            if (product.Category != null && product.Category.Status != "Active") return null;
            return MapToResponseDto(product);
        }

        public async Task<PagedResultDto<ProductListDto>> GetProductsAsync(ProductSearchQueryDto query)
        {
            var queryable = _repository.Query();

            // Filter buyer-facing product lists to only purchasable products.
            // Seller profile/admin flows pass SellerId/Status and should still be able to see sold/out-of-stock items.
            if (string.IsNullOrEmpty(query.SellerId))
            {
                queryable = queryable.Where(p =>
                    p.Category != null &&
                    p.Category.Status == "Active" &&
                    p.StockQuantity.HasValue &&
                    p.StockQuantity.Value > 0 &&
                    p.Status != ProductStatusEnum.Sold.ToString() &&
                    p.Status != ProductStatusEnum.Inactive.ToString());

                if (string.IsNullOrWhiteSpace(query.Status))
                {
                    queryable = queryable.Where(p => p.Status == ProductStatusEnum.Accepted.ToString());
                }
            }

            // Filter Category and Subcategories
            if (!string.IsNullOrEmpty(query.CategoryId))
            {
                var allCategories = await _context.Category.ToListAsync();
                var categoryIds = GetCategoryAndChildrenIds(query.CategoryId, allCategories);
                queryable = queryable.Where(p => categoryIds.Contains(p.CategoryId));
            }

            // Search Term (Name / Description)
            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var search = query.SearchTerm.ToLower();
                queryable = queryable.Where(p => p.Name.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
            }

            // Price Range
            if (query.MinPrice.HasValue)
            {
                queryable = queryable.Where(p => p.Price >= query.MinPrice);
            }
            if (query.MaxPrice.HasValue)
            {
                queryable = queryable.Where(p => p.Price <= query.MaxPrice);
            }

            // Condition
            if (!string.IsNullOrEmpty(query.Condition))
            {
                queryable = queryable.Where(p => p.Condition == query.Condition);
            }

            // Status & IsDeleted filter
            if (!string.IsNullOrEmpty(query.Status) && query.Status.Equals("Deleted", StringComparison.OrdinalIgnoreCase))
            {
                queryable = queryable.Where(p => p.IsDeleted == true);
            }
            else
            {
                queryable = queryable.Where(p => p.IsDeleted != true);
                if (!string.IsNullOrEmpty(query.Status))
                {
                    if (query.Status.Equals("Ready", StringComparison.OrdinalIgnoreCase) || query.Status.Equals("Auction", StringComparison.OrdinalIgnoreCase))
                    {
                        queryable = queryable.Where(p => p.Status == "Ready" || p.Auction.Any());
                    }
                    else
                    {
                        queryable = queryable.Where(p => p.Status == query.Status);
                    }
                }
            }

            // SellerId
            if (!string.IsNullOrEmpty(query.SellerId))
            {
                queryable = queryable.Where(p => p.SellerId == query.SellerId);
            }

            // Priority Only (Sellers with active priority subscription package - hourly randomized)
            if (query.IsPriorityOnly == true)
            {
                var now = DateTime.UtcNow;
                var activePriorityUserIds = await _context.MyService
                    .Where(s => s.Status == "Active" && (s.ServiceId == "SERVICE_PRIORITY_LISTING" || s.ServiceId == "sub_20260701_100003") && s.EndDate >= now)
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToListAsync();

                var priorityProductsList = await _context.Product
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Include(p => p.ProductImage).ThenInclude(pi => pi.Image)
                    .Where(p => p.Status == "Accepted" && p.IsDeleted != true && activePriorityUserIds.Contains(p.SellerId))
                    .ToListAsync();

                int targetCount = query.PageSize > 0 ? query.PageSize : 8;

                // If not enough priority seller items, complement with other active products
                if (priorityProductsList.Count < targetCount)
                {
                    var existingIds = priorityProductsList.Select(p => p.ProductId).ToHashSet();
                    var fillerItems = await _context.Product
                        .Include(p => p.Category)
                        .Include(p => p.Seller)
                        .Include(p => p.ProductImage).ThenInclude(pi => pi.Image)
                        .Where(p => p.Status == "Accepted" && p.IsDeleted != true && !existingIds.Contains(p.ProductId))
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(targetCount * 2)
                        .ToListAsync();

                    priorityProductsList.AddRange(fillerItems);
                }

                // Deterministic Hourly Seed Shuffle (changes every 1 hour automatically)
                int hourSeed = (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerHour);
                var rng = new Random(hourSeed);
                var shuffledItems = priorityProductsList
                    .OrderBy(_ => rng.Next())
                    .Take(targetCount)
                    .Select(p => new ProductListDto
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        CategoryName = p.Category != null ? p.Category.Name : null,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Status = p.IsDeleted == true ? "Deleted" : p.Status,
                        Condition = p.Condition,
                        CreatedAt = p.CreatedAt,
                        SellerId = p.SellerId,
                        SellerName = p.Seller != null ? $"{p.Seller.FirstName} {p.Seller.LastName}".Trim() : null,
                        MainImageUrl = p.ProductImage.Where(pi => pi.IsMain == true).Select(pi => pi.Image.ImageUrl).FirstOrDefault()
                                       ?? p.ProductImage.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.ImageUrl).FirstOrDefault(),
                        IsDeleted = p.IsDeleted
                    })
                    .ToList();

                return new PagedResultDto<ProductListDto>
                {
                    Items = shuffledItems,
                    TotalItems = shuffledItems.Count,
                    Page = 1,
                    PageSize = targetCount,
                    TotalPages = 1
                };
            }

            int totalItems = await queryable.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / query.PageSize);
            if (totalPages == 0) totalPages = 1;

            var items = await queryable
                .OrderByDynamic(query.SortBy)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new ProductListDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Status = p.IsDeleted == true ? "Deleted" : p.Status,
                    Condition = p.Condition,
                    CreatedAt = p.CreatedAt,
                    SellerId = p.SellerId,
                    SellerName = p.Seller != null ? $"{p.Seller.FirstName} {p.Seller.LastName}".Trim() : null,
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

        public async Task DeleteProductAsync(string productId, string accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Account does not exist.");

            var product = await _context.Product.FirstOrDefaultAsync(p => p.ProductId == productId && p.IsDeleted != true);
            if (product == null)
                throw new Exception("Product does not exist.");

            // Admin can delete, or Seller owns the product
            var isSellerOwner = product.SellerId == account.UserId;
            var isAdmin = account.AccountRole.Any(ar => ar.Role != null && ar.Role.Name == "Admin");

            if (!isSellerOwner && !isAdmin)
                throw new Exception("You do not have permission to delete this product.");

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            _context.Product.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task HideProductsBySellerAsync(string sellerId, DateTime updatedAt)
        {
            var products = await _context.Product
                .Where(product => product.SellerId == sellerId && product.IsDeleted != true)
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsDeleted = true;
                product.UpdatedAt = updatedAt;
            }

            if (products.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        #region Helper Methods

        private List<string> GetCategoryAndChildrenIds(string parentId, List<Category> allCategories)
        {
            var result = new List<string> { parentId };
            var children = allCategories.Where(c => c.ParentId == parentId).ToList();
            foreach (var child in children)
            {
                result.AddRange(GetCategoryAndChildrenIds(child.CategoryId, allCategories));
            }
            return result.Distinct().ToList();
        }

        private Task<string> GenerateProductIdAsync()
        {
            return Task.FromResult(RetradeBE.Utils.IdGenerator.GenerateId("prd"));
        }

        private Task<string> GenerateImageIdAsync()
        {
            return Task.FromResult(RetradeBE.Utils.IdGenerator.GenerateId("img"));
        }

        private Task<string> GenerateProductAttributeIdAsync()
        {
            return Task.FromResult(RetradeBE.Utils.IdGenerator.GenerateId("pa"));
        }

        private ProductResponseDto MapToResponseDto(Product product)
        {
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

        private void ValidateProductAttributes(Category category, List<ProductAttributeValueDto> attributeDtos)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            var activeAttributes = (category.Attributes ?? new List<Attributes>())
                .Where(a => a.IsDeleted != true)
                .ToList();

            var dtosDict = attributeDtos?
                .Where(dto => !string.IsNullOrEmpty(dto.AttributeId))
                .ToDictionary(dto => dto.AttributeId!, dto => dto.Value) ?? new Dictionary<string, string?>();

            foreach (var attribute in activeAttributes)
            {
                var isRequired = attribute.IsRequired ?? false;
                dtosDict.TryGetValue(attribute.AttributeId, out var value);
                var isProvided = !string.IsNullOrWhiteSpace(value);

                if (isRequired && !isProvided)
                {
                    throw new Exception($"Attribute '{attribute.Name}' is required.");
                }

                if (isProvided)
                {
                    if (string.Equals(attribute.DataType, "Number", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!decimal.TryParse(value, out decimal numericValue))
                        {
                            throw new Exception($"Attribute '{attribute.Name}' must be a valid number.");
                        }

                        if (attribute.MinValue.HasValue && numericValue < attribute.MinValue.Value)
                        {
                            throw new Exception($"Attribute '{attribute.Name}' must be greater than or equal to {attribute.MinValue.Value}.");
                        }

                        if (attribute.MaxValue.HasValue && numericValue > attribute.MaxValue.Value)
                        {
                            throw new Exception($"Attribute '{attribute.Name}' must be less than or equal to {attribute.MaxValue.Value}.");
                        }
                    }
                }
            }
        }

        #endregion
    }

    internal static class ProductQueryExtensions
    {
        public static IQueryable<Product> OrderByDynamic(this IQueryable<Product> query, string? sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "oldest" => query.OrderBy(p => p.CreatedAt),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "sales_desc" or "top_seller" or "popular" => query.OrderByDescending(p => p.Order.Count(o => o.OrderStatus == "Completed" || o.OrderStatus == "Delivered" || o.OrderStatus == "Confirmed" || o.OrderStatus == "Shipping")).ThenByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt), // "newest" or default
            };
        }
    }
}
