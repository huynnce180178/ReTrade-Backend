using AutoMapper;
using AutoMapper.QueryableExtensions;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class ReportService : IReportService
    {
        private const string PendingStatus = "Pending";
        private const string RejectedStatus = "Rejected";
        private const string AcceptedStatus = "Accepted";
        private const string ReviewTargetType = "Review";
        private const string BuyerTargetType = "Buyer";
        private const string SellerTargetType = "Seller";
        private const string ProductTargetType = "Product";
        private static readonly HashSet<string> AllowedReportOrderStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Delivered",
            "DeliveryFailed",
            "Completed",
            "ReturnRequested",
            "ReturnRejected",
            "Returned"
        };

        private readonly IReportRepository _reportRepository;

        private readonly IOrderService _orderService;
        private readonly IAccountService _accountService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly RetradeBE.Data.AppDbContext _context;

        public ReportService(
            IReportRepository reportRepository,
            IOrderService orderService,
            IAccountService accountService,
            IUserService userService,
            IProductService productService,
            IReviewService reviewService,
            IMapper mapper,
            INotificationService notificationService,
            RetradeBE.Data.AppDbContext context)
        {
            _reportRepository = reportRepository;
            _orderService = orderService;
            _accountService = accountService;
            _userService = userService;
            _productService = productService;
            _reviewService = reviewService;
            _mapper = mapper;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task<ReportDto> ReportReviewAsync(string accountId, string reviewId, ReportCreateDto request)
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

            var review = await _reviewService.GetByIdForReportAsync(reviewId);
            if (review == null)
            {
                throw new KeyNotFoundException("Review not found.");
            }

            if (review.IsDeleted == true)
            {
                throw new InvalidOperationException("The review has already been hidden.");
            }

            if (await _reportRepository.ExistsAsync(review.ReviewId, reporterId, ReviewTargetType))
            {
                throw new InvalidOperationException("You have already reported this review.");
            }

            var report = new Report
            {
                ReportId = RetradeBE.Utils.IdGenerator.GenerateReportId(ReviewTargetType),
                ReporterId = reporterId,
                TargetType = ReviewTargetType,
                TargetId = review.ReviewId,
                Reason = request.Reason.Trim(),
                Description = request.Description?.Trim(),
                Status = PendingStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);

            await _notificationService.NotifyAdminsAsync(
                "New Report Submitted",
                "A new report is waiting for your review.",
                "Report",
                report.ReportId
            );

            return _mapper.Map<ReportDto>(report);
        }

        public async Task<ReportDto> ReportBuyerAsync(string accountId, string orderId, ReportCreateDto request)
        {
            var reporterId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(reporterId))
            {
                throw new UnauthorizedAccessException("Account not found.");
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new InvalidOperationException("OrderId is required.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("Report reason is required.");
            }

            var order = await _orderService.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            if (string.IsNullOrWhiteSpace(order.Status) || !AllowedReportOrderStatuses.Contains(order.Status))
            {
                throw new InvalidOperationException("Orders can only be reported after delivery or completion.");
            }

            if (string.IsNullOrWhiteSpace(order.SellerId) || string.IsNullOrWhiteSpace(order.BuyerId))
            {
                throw new InvalidOperationException("The order does not have a valid buyer or seller.");
            }

            if (!string.Equals(order.SellerId, reporterId, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only the seller of the order can report the buyer.");
            }

            var buyer = await _userService.GetByIdAsync(order.BuyerId);
            if (buyer == null)
            {
                throw new KeyNotFoundException("Buyer not found.");
            }

            if (await _reportRepository.ExistsAsync(order.OrderId, reporterId, BuyerTargetType))
            {
                throw new InvalidOperationException("You have already reported this buyer for this order.");
            }

            var report = new Report
            {
                ReportId = RetradeBE.Utils.IdGenerator.GenerateReportId(BuyerTargetType),
                ReporterId = reporterId,
                TargetType = BuyerTargetType,
                TargetId = order.OrderId,
                Reason = request.Reason.Trim(),
                Description = request.Description?.Trim(),
                Status = PendingStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);

            await _notificationService.NotifyAdminsAsync(
                "New Report Submitted",
                "A new report is waiting for your review.",
                "Report",
                report.ReportId
            );

            return _mapper.Map<ReportDto>(report);
        }

        public async Task<ReportDto> ReportSellerAsync(string accountId, string orderId, ReportCreateDto request)
        {
            var reporterId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(reporterId))
            {
                throw new UnauthorizedAccessException("Account not found.");
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new InvalidOperationException("OrderId is required.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("Report reason is required.");
            }

            var order = await _orderService.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            if (string.IsNullOrWhiteSpace(order.Status) || !AllowedReportOrderStatuses.Contains(order.Status))
            {
                throw new InvalidOperationException("Orders can only be reported after delivery or completion.");
            }

            if (string.IsNullOrWhiteSpace(order.SellerId) || string.IsNullOrWhiteSpace(order.BuyerId))
            {
                throw new InvalidOperationException("The order does not have a valid buyer or seller.");
            }

            if (!string.Equals(order.BuyerId, reporterId, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only the buyer of the order can report the seller.");
            }

            var seller = await _userService.GetByIdAsync(order.SellerId);
            if (seller == null)
            {
                throw new KeyNotFoundException("Seller not found.");
            }

            if (await _reportRepository.ExistsAsync(order.OrderId, reporterId, SellerTargetType))
            {
                throw new InvalidOperationException("You have already reported this seller for this order.");
            }

            var report = new Report
            {
                ReportId = RetradeBE.Utils.IdGenerator.GenerateReportId(SellerTargetType),
                ReporterId = reporterId,
                TargetType = SellerTargetType,
                TargetId = order.OrderId,
                Reason = request.Reason.Trim(),
                Description = request.Description?.Trim(),
                Status = PendingStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);

            await _notificationService.NotifyAdminsAsync(
                "New Report Submitted",
                "A new report is waiting for your review.",
                "Report",
                report.ReportId
            );

            return _mapper.Map<ReportDto>(report);
        }

        public async Task<ReportDto> ReportProductAsync(string accountId, string productId, ReportCreateDto request)
        {
            var reporterId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(reporterId))
            {
                throw new UnauthorizedAccessException("Account not found.");
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new InvalidOperationException("ProductId is required.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("Report reason is required.");
            }

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                throw new KeyNotFoundException("Product not found.");
            }

            if (string.Equals(product.SellerId, reporterId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("You cannot report your own product.");
            }

            if (await _reportRepository.ExistsAsync(productId, reporterId, ProductTargetType))
            {
                throw new InvalidOperationException("You have already reported this product.");
            }

            var report = new Report
            {
                ReportId = RetradeBE.Utils.IdGenerator.GenerateReportId(ProductTargetType),
                ReporterId = reporterId,
                TargetType = ProductTargetType,
                TargetId = productId,
                Reason = request.Reason.Trim(),
                Description = request.Description?.Trim(),
                Status = PendingStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);

            await _notificationService.NotifyAdminsAsync(
                "New Report Submitted",
                "A new product report is waiting for your review.",
                "Report",
                report.ReportId
            );

            return _mapper.Map<ReportDto>(report);
        }

        public async Task<IQueryable<ReportListDto>> GetAllAsync()
        {
            var reports = _reportRepository.Query();
            return await Task.FromResult(reports.ProjectTo<ReportListDto>(_mapper.ConfigurationProvider));
        }

        public async Task<ReportDetailDto?> GetByIdAsync(string reportId)
        {
            var report = await _reportRepository.GetByIdAsync(reportId);
            if (report == null)
            {
                return null;
            }

            var detail = _mapper.Map<ReportDetailDto>(report);

            if (string.Equals(report.TargetType, ReviewTargetType, StringComparison.OrdinalIgnoreCase))
            {
                var review = await _reviewService.GetByIdForReportAsync(report.TargetId);
                detail.Review = review == null ? null : _mapper.Map<ReportReviewDetailDto>(review);
            }
            else if (string.Equals(report.TargetType, ProductTargetType, StringComparison.OrdinalIgnoreCase)
                  || string.Equals(report.TargetType, "ProductAppeal", StringComparison.OrdinalIgnoreCase))
            {
                var product = await _productService.GetProductByIdAsync(report.TargetId);
                if (product != null)
                {
                    detail.Product = _mapper.Map<ReportProductDetailDto>(product);
                }
            }
            else if (string.Equals(report.TargetType, BuyerTargetType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(report.TargetType, SellerTargetType, StringComparison.OrdinalIgnoreCase))
            {
                var order = await _orderService.GetByIdAsync(report.TargetId);
                if (order != null)
                {
                    detail.Order = _mapper.Map<ReportOrderDetailDto>(order);

                    if (string.Equals(report.TargetType, BuyerTargetType, StringComparison.OrdinalIgnoreCase))
                    {
                        var buyer = order.BuyerId == null ? null : await _userService.GetByIdAsync(order.BuyerId);
                        detail.Buyer = buyer == null ? null : _mapper.Map<ReportUserDetailDto>(buyer);
                    }
                    else
                    {
                        var seller = order.SellerId == null ? null : await _userService.GetByIdAsync(order.SellerId);
                        detail.Seller = seller == null ? null : _mapper.Map<ReportUserDetailDto>(seller);
                    }
                }
            }

            return detail;
        }

        public async Task<ReportDto?> UpdateStatusAsync(string reportId, ReportStatusUpdateDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                throw new InvalidOperationException("Status is required.");
            }

            var report = await _reportRepository.GetByIdAsync(reportId);
            if (report == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;

            if (string.Equals(request.Status, "Reject", StringComparison.OrdinalIgnoreCase))
            {
                report.Status = RejectedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;
                await _reportRepository.UpdateAsync(report);

                try
                {
                    string targetName = report.TargetType;
                    if (string.Equals(report.TargetType, ProductTargetType, StringComparison.OrdinalIgnoreCase))
                    {
                        var product = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_context.Product, p => p.ProductId == report.TargetId);
                        if (product != null)
                        {
                            targetName = $"product '{product.Name}'";
                        }
                    }

                    await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                    {
                        UserId = report.ReporterId,
                        Title = "Report Reviewed",
                        Message = $"Your report regarding {targetName} has been reviewed and rejected.",
                        Type = nameof(NotificationTypeEnum.Report),
                        ReferenceId = report.ReportId
                    });
                }
                catch { }

                return _mapper.Map<ReportDto>(report);
            }

            if (string.Equals(request.Status, "Accept Review", StringComparison.OrdinalIgnoreCase))
            {
                report.Status = AcceptedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;

                await _reviewService.HideForReportAsync(report.TargetId, now);

                await _reportRepository.UpdateAsync(report);

                try
                {
                    await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                    {
                        UserId = report.ReporterId,
                        Title = "Report Approved",
                        Message = "Thank you! Your report regarding the review has been reviewed and approved. The violating review has been removed.",
                        Type = nameof(NotificationTypeEnum.Report),
                        ReferenceId = report.ReportId
                    });

                    // TargetId here is the ReviewId.
                    var review = await _reviewService.GetByIdForReportAsync(report.TargetId);
                    if (review != null)
                    {
                        await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                        {
                            UserId = review.ReviewerId,
                            Title = "Review Removed",
                            Message = "Your review has been removed due to a violation of community guidelines.",
                            Type = nameof(NotificationTypeEnum.System),
                            ReferenceId = review.ReviewId
                        });
                    }
                }
                catch { }

                return _mapper.Map<ReportDto>(report);
            }

            if (string.Equals(request.Status, "Accept Buyer", StringComparison.OrdinalIgnoreCase))
            {
                report.Status = AcceptedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;

                var order = await _orderService.GetByIdAsync(report.TargetId);
                if (order != null && !string.IsNullOrWhiteSpace(order.BuyerId))
                {
                    var buyer = await _userService.GetByIdAsync(order.BuyerId);
                    if (buyer != null)
                    {
                        buyer.FlagCount = (buyer.FlagCount ?? 0) + 1;
                        buyer.UpdatedAt = now;
                        await _userService.UpdateAsync(buyer);

                        if ((buyer.FlagCount ?? 0) >= 2)
                        {
                            var account = await _accountService.GetByUserIdAsync(buyer.UserId);
                            if (account != null)
                            {
                                if (account.Status != RetradeBE.Models.Enums.AccountStatusEnum.Ban.ToString())
                                {
                                    await _accountService.BanUserAsync(account.AccountId);
                                }
                            }

                            buyer.IsDeleted = true;
                            buyer.UpdatedAt = now;

                            await _userService.UpdateAsync(buyer);
                            await _productService.HideProductsBySellerAsync(buyer.UserId, now);
                        }
                    }
                }

                await _reportRepository.UpdateAsync(report);

                try
                {
                    await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                    {
                        UserId = report.ReporterId,
                        Title = "Report Approved",
                        Message = "Thank you! Your report regarding the buyer has been approved and action has been taken.",
                        Type = nameof(NotificationTypeEnum.Report),
                        ReferenceId = report.ReportId
                    });

                    if (order != null && !string.IsNullOrWhiteSpace(order.BuyerId))
                    {
                        await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                        {
                            UserId = order.BuyerId,
                            Title = "Account Violation Warning",
                            Message = "Your account has received a violation penalty due to a policy report.",
                            Type = nameof(NotificationTypeEnum.System),
                            ReferenceId = report.ReportId
                        });
                    }
                }
                catch { }

                return _mapper.Map<ReportDto>(report);
            }

            if (string.Equals(request.Status, "Accept Seller", StringComparison.OrdinalIgnoreCase))
            {
                report.Status = AcceptedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;

                var order = await _orderService.GetByIdAsync(report.TargetId);
                if (order != null && !string.IsNullOrWhiteSpace(order.SellerId))
                {
                    var seller = await _userService.GetByIdAsync(order.SellerId);
                    if (seller != null)
                    {
                        var account = await _accountService.GetByUserIdAsync(seller.UserId);
                        if (account != null)
                        {
                            if (account.Status != RetradeBE.Models.Enums.AccountStatusEnum.Ban.ToString())
                            {
                                await _accountService.BanUserAsync(account.AccountId);
                            }
                        }

                        seller.IsDeleted = true;
                        seller.UpdatedAt = now;
                        
                        await _userService.UpdateAsync(seller);
                        await _productService.HideProductsBySellerAsync(seller.UserId, now);
                    }
                }

                await _reportRepository.UpdateAsync(report);

                try
                {
                    await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                    {
                        UserId = report.ReporterId,
                        Title = "Report Approved",
                        Message = "Thank you! Your report regarding the seller has been approved and action has been taken.",
                        Type = nameof(NotificationTypeEnum.Report),
                        ReferenceId = report.ReportId
                    });

                    if (order != null && !string.IsNullOrWhiteSpace(order.SellerId))
                    {
                        await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                        {
                            UserId = order.SellerId,
                            Title = "Account Violation Warning",
                            Message = "Your account has received a violation penalty due to a policy report.",
                            Type = nameof(NotificationTypeEnum.System),
                            ReferenceId = report.ReportId
                        });
                    }
                }
                catch { }

                return _mapper.Map<ReportDto>(report);
            }

            if (string.Equals(request.Status, "Accept Product", StringComparison.OrdinalIgnoreCase))
            {
                report.Status = AcceptedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;

                var dbProduct = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_context.Product, p => p.ProductId == report.TargetId);
                if (dbProduct != null)
                {
                    dbProduct.Status = ProductStatusEnum.Removed.ToString();
                    dbProduct.UpdatedAt = now;
                    _context.Product.Update(dbProduct);
                    await _context.SaveChangesAsync();

                    try
                    {
                        await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                        {
                            UserId = dbProduct.SellerId,
                            Title = "Product Unlisted",
                            Message = $"Your product '{dbProduct.Name}' has been unlisted from the platform due to a violation of community guidelines.",
                            Type = nameof(NotificationTypeEnum.System),
                            ReferenceId = dbProduct.ProductId
                        });
                        await _notificationService.BroadcastProductUnlistedAsync(dbProduct.ProductId);
                    }
                    catch { }
                }

                await _reportRepository.UpdateAsync(report);

                try
                {
                    await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                    {
                        UserId = report.ReporterId,
                        Title = "Report Approved",
                        Message = $"Thank you! Your report regarding product '{dbProduct?.Name ?? report.TargetId}' has been approved. The item has been unlisted from the platform.",
                        Type = nameof(NotificationTypeEnum.Report),
                        ReferenceId = report.ReportId
                    });
                }
                catch { }

                return _mapper.Map<ReportDto>(report);
            }

            if (string.Equals(request.Status, "Accept Appeal", StringComparison.OrdinalIgnoreCase) ||
               (string.Equals(report.TargetType, "ProductAppeal", StringComparison.OrdinalIgnoreCase) && string.Equals(request.Status, "Accept Product", StringComparison.OrdinalIgnoreCase)))
            {
                report.Status = AcceptedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;

                var dbProduct = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_context.Product, p => p.ProductId == report.TargetId);
                if (dbProduct != null)
                {
                    dbProduct.Status = dbProduct.Price == null ? ProductStatusEnum.Ready.ToString() : ProductStatusEnum.Accepted.ToString();
                    dbProduct.UpdatedAt = now;
                    _context.Product.Update(dbProduct);
                    await _context.SaveChangesAsync();

                    try
                    {
                        string sellerUserId = dbProduct.SellerId;
                        var sellerAccount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_context.Account, a => a.AccountId == dbProduct.SellerId || a.UserId == dbProduct.SellerId);
                        if (sellerAccount != null && !string.IsNullOrEmpty(sellerAccount.UserId))
                        {
                            sellerUserId = sellerAccount.UserId;
                        }

                        await _notificationService.CreateAndSendAsync(new CreateNotificationDto
                        {
                            UserId = sellerUserId,
                            Title = "Product Appeal Approved",
                            Message = $"Great news! Your appeal for product '{dbProduct.Name}' has been approved by Administrator and the product has been restored.",
                            Type = nameof(NotificationTypeEnum.System),
                            ReferenceId = dbProduct.ProductId
                        });
                        await _notificationService.BroadcastProductRestoredAsync(dbProduct.ProductId, dbProduct.Status);
                    }
                    catch { }
                }

                await _reportRepository.UpdateAsync(report);
                return _mapper.Map<ReportDto>(report);
            }

            throw new InvalidOperationException("Unsupported report status.");
        }

        public async Task<IReadOnlyList<FlaggedUserDto>> GetFlaggedUsersAsync()
        {
            var users = await _userService.GetAllAsync();
            var flaggedUsers = users
                .Where(user => (user.FlagCount ?? 0) > 0)
                .OrderByDescending(user => user.FlagCount)
                .ToList();

            var result = new List<FlaggedUserDto>();

            foreach (var user in flaggedUsers)
            {
                var reports = await _reportRepository.GetReportsForUserAsync(user.UserId);
                var flaggedUser = _mapper.Map<FlaggedUserDto>(user);
                flaggedUser.Reports = _mapper.Map<List<ReportListDto>>(reports);
                result.Add(flaggedUser);
            }

            return result;
        }

        public async Task<ReportHistoryDto> GetHistoryAsync(string accountId)
        {
            var reporterId = await ResolveUserIdAsync(accountId);
            if (string.IsNullOrWhiteSpace(reporterId))
            {
                return new ReportHistoryDto();
            }

            var submitted = await _reportRepository.GetReportsByReporterAsync(reporterId);
            var received = await _reportRepository.GetReportsReceivedByUserAsync(reporterId);

            return new ReportHistoryDto
            {
                ReportsSubmitted = _mapper.Map<List<ReportListDto>>(submitted),
                ReportsReceived = _mapper.Map<List<ReportListDto>>(received)
            };
        }
        private async Task<string?> ResolveUserIdAsync(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return null;
            }

            var account = await _accountService.GetByIdAsync(accountId);
            return account?.UserId;
        }

    }
}
