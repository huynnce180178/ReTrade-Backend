using Microsoft.EntityFrameworkCore;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class ReviewService : IReviewService
    {
        private const string CompletedStatus = "Completed";
        private const string PendingReportStatus = "Pending";
        private const string ReviewReportTargetType = "Review";

        private readonly IOrderRepository _orderRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IReportRepository _reportRepository;
        private readonly IAccountRepository _accountRepository;

        public ReviewService(
            IOrderRepository orderRepository,
            IReviewRepository reviewRepository,
            IReportRepository reportRepository,
            IAccountRepository accountRepository)
        {
            _orderRepository = orderRepository;
            _reviewRepository = reviewRepository;
            _reportRepository = reportRepository;
            _accountRepository = accountRepository;
        }

        public async Task<ReviewResponseDto?> GetByBuyerOrderAsync(string accountId, string buyerId, string orderId, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(buyerId) || string.IsNullOrWhiteSpace(orderId))
            {
                return null;
            }

            var requesterId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(requesterId))
            {
                throw new UnauthorizedAccessException("Account not found.");
            }

            if (!isAdmin && requesterId != buyerId)
            {
                throw new UnauthorizedAccessException("You can only view reviews for your own orders.");
            }

            var review = await _reviewRepository.Query()
                .FirstOrDefaultAsync(item => item.ReviewerId == buyerId && item.OrderId == orderId);
            return review == null
                ? null
                : MapReview(review, new List<Report>(), requesterId, includeReports: false, includeReviewerPrivateInfo: true);
        }

        public async Task<PagedResultDto<ReviewResponseDto>> GetSellerReviewsAsync(string accountId, ReviewQueryDto query)
        {
            var sellerId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                return EmptyPagedResult(query);
            }

            var reviews = GetSellerReviewsQuery(sellerId);
            return await ToPagedReviewsAsync(reviews, query, sellerId, includeReports: false, includeReviewerPrivateInfo: true);
        }

        public async Task<PagedResultDto<ReviewResponseDto>> GetPublicSellerReviewsAsync(string sellerId, ReviewQueryDto query)
        {
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                return EmptyPagedResult(query);
            }

            var resolvedSellerId = await ResolveUserIdAsync(sellerId) ?? sellerId.Trim();
            return await ToPagedReviewsAsync(GetSellerReviewsQuery(resolvedSellerId), query, currentUserId: null, includeReports: false, includeReviewerPrivateInfo: false);
        }

        public async Task<ReviewSummaryDto> GetPublicSellerReviewSummaryAsync(string sellerId)
        {
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                return new ReviewSummaryDto();
            }

            var resolvedSellerId = await ResolveUserIdAsync(sellerId) ?? sellerId.Trim();
            return await BuildSummaryAsync(GetSellerReviewsQuery(resolvedSellerId));
        }

        public async Task<ReviewSummaryDto> GetSellerReviewSummaryAsync(string accountId)
        {
            var sellerId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                return new ReviewSummaryDto();
            }

            return await BuildSummaryAsync(GetSellerReviewsQuery(sellerId));
        }

        public async Task<PagedResultDto<ReviewResponseDto>> GetAdminReviewsAsync(ReviewQueryDto query)
        {
            var reviews = _reviewRepository.Query();
            if (!string.IsNullOrWhiteSpace(query.SellerId))
            {
                var sellerId = query.SellerId.Trim();
                reviews = reviews.Where(review => review.SellerId == sellerId
                    || (review.Order != null && review.Order.SellerId == sellerId));
            }

            return await ToPagedReviewsAsync(reviews, query, currentUserId: null, includeReports: true, includeReviewerPrivateInfo: true);
        }

        public async Task<ReviewSummaryDto> GetAdminReviewSummaryAsync(ReviewQueryDto query)
        {
            var reviews = _reviewRepository.Query();
            if (!string.IsNullOrWhiteSpace(query.SellerId))
            {
                var sellerId = query.SellerId.Trim();
                reviews = reviews.Where(review => review.SellerId == sellerId
                    || (review.Order != null && review.Order.SellerId == sellerId));
            }

            return await BuildSummaryAsync(reviews);
        }

        public async Task<ReviewResponseDto?> CreateAsync(string accountId, string buyerId, ReviewCreateDto request)
        {
            var authenticatedBuyerId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(authenticatedBuyerId))
            {
                throw new UnauthorizedAccessException("Account not found.");
            }

            if (!string.IsNullOrWhiteSpace(buyerId) && buyerId != authenticatedBuyerId)
            {
                throw new UnauthorizedAccessException("You can only create reviews for your own completed orders.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.OrderId))
            {
                throw new InvalidOperationException("Invalid review request.");
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                throw new InvalidOperationException("Rating must be between 1 and 5.");
            }

            var order = await _orderRepository.GetForUpdateAsync(request.OrderId);
            if (order == null || order.BuyerId != authenticatedBuyerId)
            {
                return null;
            }

            if (order.SellerId == authenticatedBuyerId)
            {
                throw new InvalidOperationException("You cannot review your own store.");
            }

            if (!string.Equals(order.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Review can only be submitted after the order is completed.");
            }

            var existingReview = await _reviewRepository.GetByBuyerOrderAsync(authenticatedBuyerId, request.OrderId);
            if (existingReview != null)
            {
                throw new InvalidOperationException("You have already reviewed this order.");
            }

            var review = new Review
            {
                ReviewId = Guid.NewGuid().ToString("N"),
                ReviewerId = authenticatedBuyerId,
                SellerId = order.SellerId,
                OrderId = order.OrderId,
                Rating = request.Rating,
                Comment = request.Comment?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(review);
            review.Order = order;
            return MapReview(review, new List<Report>(), authenticatedBuyerId, includeReports: false, includeReviewerPrivateInfo: true);
        }

        public async Task<ReportDto> ReportReviewAsync(string accountId, string reviewId, ReportCreateDto request, bool isAdmin)
        {
            var reporterId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(reporterId))
            {
                throw new UnauthorizedAccessException("Account not found.");
            }

            if (string.IsNullOrWhiteSpace(reviewId))
            {
                throw new InvalidOperationException("ReviewId is required.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("Report reason is required.");
            }

            var review = await _reviewRepository.GetByIdForReportAsync(reviewId);
            if (review == null)
            {
                throw new KeyNotFoundException("Review not found.");
            }

            var reviewSellerId = review.SellerId ?? review.Order?.SellerId;
            if (!isAdmin && reviewSellerId != reporterId)
            {
                throw new UnauthorizedAccessException("You can only report reviews for your own store.");
            }

            var existingReport = await _reportRepository.GetReportByReporterAsync(reviewId, reporterId);
            if (existingReport != null)
            {
                throw new InvalidOperationException("You have already reported this review.");
            }

            var now = DateTime.UtcNow;
            var report = new Report
            {
                ReportId = Guid.NewGuid().ToString("N"),
                TargetId = review.ReviewId, TargetType = "Review",
                ReporterId = reporterId,
                Reason = request.Reason.Trim(),
                Description = request.Description?.Trim(),
                Status = PendingReportStatus,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _reportRepository.AddReportAsync(report);
            return MapReport(report);
        }

        private IQueryable<Review> GetSellerReviewsQuery(string sellerId)
        {
            return _reviewRepository.Query()
                .Where(review => review.SellerId == sellerId
                    || (review.Order != null && review.Order.SellerId == sellerId));
        }

        private IQueryable<Review> ApplyFilters(IQueryable<Review> reviews, ReviewQueryDto query)
        {
            query.Page = query.Page < 1 ? 1 : query.Page;
            query.PageSize = query.PageSize < 1 ? 12 : Math.Min(query.PageSize, 100);
            var reviewReports = _reportRepository.Query()
                .Where(report => report.TargetType == ReviewReportTargetType);

            if (query.Rating is >= 1 and <= 5)
            {
                reviews = reviews.Where(review => review.Rating == query.Rating);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim();
                if (status.Equals("Reported", StringComparison.OrdinalIgnoreCase))
                {
                    reviews = reviews.Where(review => reviewReports.Select(report => report.TargetId).Contains(review.ReviewId));
                }
                else if (status.Equals("Unreported", StringComparison.OrdinalIgnoreCase))
                {
                    reviews = reviews.Where(review => !reviewReports.Select(report => report.TargetId).Contains(review.ReviewId));
                }
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim().ToLower();
                reviews = reviews.Where(review =>
                    (review.Comment != null && review.Comment.ToLower().Contains(term)) ||
                    (review.Order != null && review.Order.OrderCode != null && review.Order.OrderCode.ToLower().Contains(term)) ||
                    (review.Order != null && review.Order.Product != null && review.Order.Product.Name != null && review.Order.Product.Name.ToLower().Contains(term)) ||
                    (review.Reviewer != null && (
                        ((review.Reviewer.FirstName ?? "") + " " + (review.Reviewer.LastName ?? "")).ToLower().Contains(term) ||
                        (review.Reviewer.Email != null && review.Reviewer.Email.ToLower().Contains(term)))) ||
                    (review.Seller != null && (
                        ((review.Seller.FirstName ?? "") + " " + (review.Seller.LastName ?? "")).ToLower().Contains(term) ||
                        (review.Seller.Email != null && review.Seller.Email.ToLower().Contains(term)))));
            }

            reviews = query.SortBy?.ToLowerInvariant() switch
            {
                "oldest" => reviews.OrderBy(review => review.CreatedAt),
                "rating_desc" => reviews.OrderByDescending(review => review.Rating).ThenByDescending(review => review.CreatedAt),
                "rating_asc" => reviews.OrderBy(review => review.Rating).ThenByDescending(review => review.CreatedAt),
                "reported" => reviews
                    .OrderByDescending(review => reviewReports.Count(report => report.TargetId == review.ReviewId))
                    .ThenByDescending(review => review.CreatedAt),
                _ => reviews.OrderByDescending(review => review.CreatedAt)
            };

            return reviews;
        }

        private async Task<PagedResultDto<ReviewResponseDto>> ToPagedReviewsAsync(
            IQueryable<Review> reviews,
            ReviewQueryDto query,
            string? currentUserId,
            bool includeReports,
            bool includeReviewerPrivateInfo)
        {
            reviews = ApplyFilters(reviews, query);
            var totalItems = await reviews.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / query.PageSize);
            var items = await reviews
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
            var reviewIds = items.Select(review => review.ReviewId).ToList();
            var pageReports = await _reportRepository.Query()
                .Where(report => report.TargetType == ReviewReportTargetType && reviewIds.Contains(report.TargetId))
                .OrderByDescending(report => report.CreatedAt)
                .ToListAsync();
            var reportsByReviewId = pageReports
                .GroupBy(report => report.TargetId)
                .ToDictionary(group => group.Key, group => group.ToList());

            return new PagedResultDto<ReviewResponseDto>
            {
                Items = items.Select(review =>
                {
                    reportsByReviewId.TryGetValue(review.ReviewId, out var reports);
                    return MapReview(review, reports ?? new List<Report>(), currentUserId, includeReports, includeReviewerPrivateInfo);
                }).ToList(),
                TotalItems = totalItems,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = totalPages
            };
        }

        private async Task<ReviewSummaryDto> BuildSummaryAsync(IQueryable<Review> reviews)
        {
            var totalReviews = await reviews.CountAsync();
            var ratings = reviews.Where(review => review.Rating != null);
            var averageRating = await ratings.AnyAsync()
                ? await ratings.AverageAsync(review => review.Rating!.Value)
                : 0;
            var reviewIds = reviews.Select(review => review.ReviewId);
            var reportedReviews = await _reportRepository.Query()
                .Where(report => report.TargetType == ReviewReportTargetType && reviewIds.Contains(report.TargetId))
                .Select(report => report.TargetId)
                .Distinct()
                .CountAsync();
            var ratingStats = await ratings
                .GroupBy(review => review.Rating!.Value)
                .Select(group => new { Rating = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Rating, item => item.Count);

            return new ReviewSummaryDto
            {
                TotalReviews = totalReviews,
                AverageRating = Math.Round(averageRating, 2),
                ReportedReviews = reportedReviews,
                RatingStats = Enumerable.Range(1, 5).ToDictionary(rating => rating, rating => ratingStats.GetValueOrDefault(rating))
            };
        }

        private async Task<string?> ResolveUserIdAsync(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return null;
            }

            var account = await _accountRepository.GetByIdAsync(accountId);
            return account?.UserId;
        }

        private static ReviewResponseDto MapReview(
            Review review,
            List<Report> reviewReports,
            string? currentUserId,
            bool includeReports,
            bool includeReviewerPrivateInfo)
        {
            var product = review.Order?.Product;
            var seller = review.Seller ?? review.Order?.Seller;
            var reviewer = review.Reviewer ?? review.Order?.Buyer;
            var reports = reviewReports
                .OrderByDescending(report => report.CreatedAt)
                .Select(MapReport)
                .ToList();
            var currentUserReport = !string.IsNullOrWhiteSpace(currentUserId)
                ? reports.FirstOrDefault(report => report.ReporterId == currentUserId)
                : null;
            var latestReport = reports.FirstOrDefault();

            return new ReviewResponseDto
            {
                ReviewId = review.ReviewId,
                TargetType = ReviewReportTargetType,
                ReviewerId = review.ReviewerId,
                ReviewerName = FormatUserName(reviewer),
                ReviewerEmail = includeReviewerPrivateInfo ? reviewer?.Email : null,
                ReviewerAvatarUrl = includeReviewerPrivateInfo ? reviewer?.AvatarUrl : null,
                SellerId = review.SellerId ?? review.Order?.SellerId,
                SellerName = FormatUserName(seller),
                OrderId = review.OrderId,
                OrderCode = review.Order?.OrderCode,
                ProductId = product?.ProductId,
                ProductName = product?.Name,
                ProductImageUrl = GetProductImageUrl(product),
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                ReportCount = reports.Count,
                ReportedByCurrentUser = currentUserReport != null,
                LatestReportStatus = latestReport?.Status,
                LatestReportReason = latestReport?.Reason,
                LatestReportCreatedAt = latestReport?.CreatedAt,
                CurrentUserReport = currentUserReport,
                Reports = includeReports ? reports : new List<ReportDto>()
            };
        }

        private static ReportDto MapReport(Report report)
        {
            return new ReportDto
            {
                ReportId = report.ReportId,
                TargetId = report.TargetId,
                TargetType = report.TargetType,
                ReporterId = report.ReporterId,
                ReporterName = FormatUserName(report.Reporter),
                Reason = report.Reason,
                Description = report.Description,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }

        private static string? FormatUserName(User? user)
        {
            if (user == null) return null;
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
        }

        private static string? GetProductImageUrl(Product? product)
        {
            if (product == null) return null;

            return product.ProductImage
                .Where(image => image.IsMain == true)
                .Select(image => image.Image.ImageUrl)
                .FirstOrDefault()
                ?? product.ProductImage
                    .OrderBy(image => image.SortOrder)
                    .Select(image => image.Image.ImageUrl)
                    .FirstOrDefault();
        }

        private static PagedResultDto<ReviewResponseDto> EmptyPagedResult(ReviewQueryDto query)
        {
            return new PagedResultDto<ReviewResponseDto>
            {
                Items = new List<ReviewResponseDto>(),
                TotalItems = 0,
                Page = query.Page < 1 ? 1 : query.Page,
                PageSize = query.PageSize < 1 ? 12 : Math.Min(query.PageSize, 100),
                TotalPages = 0
            };
        }
    }
}

