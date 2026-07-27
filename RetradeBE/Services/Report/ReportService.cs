using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class ReportService : IReportService
    {
        private const string PendingStatus = "Pending";
        private const string RejectedStatus = "Rejected";
        private const string AcceptedStatus = "Accepted";
        private const string ReviewTargetType = "review";
        private const string BuyerTargetType = "buyer";
        private const string SellerTargetType = "seller";
        private const string CompletedStatus = "Completed";

        private readonly IReportRepository _reportRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;

        public ReportService(
            IReportRepository reportRepository,
            IOrderRepository orderRepository,
            IAccountRepository accountRepository,
            IUserRepository userRepository,
            IProductRepository productRepository,
            IAccountService accountService,
            IMapper mapper)
        {
            _reportRepository = reportRepository;
            _orderRepository = orderRepository;
            _accountRepository = accountRepository;
            _userRepository = userRepository;
            _productRepository = productRepository;
            _accountService = accountService;
            _mapper = mapper;
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

            var review = await _reportRepository.GetReviewByIdAsync(reviewId);
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
                ReportId = Guid.NewGuid().ToString("N"),
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

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            if (!string.Equals(order.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only completed orders can be reported.");
            }

            if (string.IsNullOrWhiteSpace(order.SellerId) || string.IsNullOrWhiteSpace(order.BuyerId))
            {
                throw new InvalidOperationException("The order does not have a valid buyer or seller.");
            }

            if (!string.Equals(order.SellerId, reporterId, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only the seller of the order can report the buyer.");
            }

            var buyer = await _userRepository.GetByIdAsync(order.BuyerId);
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
                ReportId = Guid.NewGuid().ToString("N"),
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

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            if (!string.Equals(order.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only completed orders can be reported.");
            }

            if (string.IsNullOrWhiteSpace(order.SellerId) || string.IsNullOrWhiteSpace(order.BuyerId))
            {
                throw new InvalidOperationException("The order does not have a valid buyer or seller.");
            }

            if (!string.Equals(order.BuyerId, reporterId, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only the buyer of the order can report the seller.");
            }

            var seller = await _userRepository.GetByIdAsync(order.SellerId);
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
                ReportId = Guid.NewGuid().ToString("N"),
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
                var review = await _reportRepository.GetReviewByIdAsync(report.TargetId);
                detail.Review = review == null ? null : _mapper.Map<ReportReviewDetailDto>(review);
            }
            else if (string.Equals(report.TargetType, BuyerTargetType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(report.TargetType, SellerTargetType, StringComparison.OrdinalIgnoreCase))
            {
                var order = await _orderRepository.GetByIdAsync(report.TargetId);
                if (order != null)
                {
                    detail.Order = _mapper.Map<ReportOrderDetailDto>(order);

                    if (string.Equals(report.TargetType, BuyerTargetType, StringComparison.OrdinalIgnoreCase))
                    {
                        var buyer = order.BuyerId == null ? null : await _userRepository.GetByIdAsync(order.BuyerId);
                        detail.Buyer = buyer == null ? null : _mapper.Map<ReportUserDetailDto>(buyer);
                    }
                    else
                    {
                        var seller = order.SellerId == null ? null : await _userRepository.GetByIdAsync(order.SellerId);
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
                return _mapper.Map<ReportDto>(report);
            }

            if (string.Equals(request.Status, "Accept Review", StringComparison.OrdinalIgnoreCase))
            {
                report.Status = AcceptedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;

                var review = await _reportRepository.GetReviewByIdAsync(report.TargetId);
                if (review != null)
                {
                    review.IsDeleted = true;
                    review.UpdatedAt = now;
                    await _reportRepository.UpdateReviewAsync(review);
                }

                await _reportRepository.UpdateAsync(report);
                return _mapper.Map<ReportDto>(report);
            }

            if (string.Equals(request.Status, "Accept Buyer", StringComparison.OrdinalIgnoreCase))
            {
                report.Status = AcceptedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;

                var order = await _orderRepository.GetByIdAsync(report.TargetId);
                if (order != null && !string.IsNullOrWhiteSpace(order.BuyerId))
                {
                    var buyer = await _userRepository.GetByIdAsync(order.BuyerId);
                    if (buyer != null)
                    {
                        buyer.FlagCount = (buyer.FlagCount ?? 0) + 1;
                        buyer.UpdatedAt = now;
                        await _userRepository.UpdateAsync(buyer);

                        if ((buyer.FlagCount ?? 0) >= 2)
                        {
                            await ApplyUserBanAndHideAsync(buyer.UserId, now);
                        }
                    }
                }

                await _reportRepository.UpdateAsync(report);
                return _mapper.Map<ReportDto>(report);
            }

            if (string.Equals(request.Status, "Accept Seller", StringComparison.OrdinalIgnoreCase))
            {
                report.Status = AcceptedStatus;
                report.ReviewedAt = now;
                report.UpdatedAt = now;

                var order = await _orderRepository.GetByIdAsync(report.TargetId);
                if (order != null && !string.IsNullOrWhiteSpace(order.SellerId))
                {
                    var seller = await _userRepository.GetByIdAsync(order.SellerId);
                    if (seller != null)
                    {
                        await ApplyUserBanAndHideAsync(seller.UserId, now);
                    }
                }

                await _reportRepository.UpdateAsync(report);
                return _mapper.Map<ReportDto>(report);
            }

            throw new InvalidOperationException("Unsupported report status.");
        }

        public async Task<IReadOnlyList<FlaggedUserDto>> GetFlaggedUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
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

        private async Task ApplyUserBanAndHideAsync(string userId, DateTime now)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return;
            }

            var account = await _accountRepository.GetByIdAsync(user.UserId);
            if (account != null)
            {
                await _accountService.BanUserAsync(account.AccountId);
                account.IsDeleted = true;
                account.UpdatedAt = now;
                await _accountRepository.UpdateAsync(account);
            }

            user.IsDeleted = true;
            user.UpdatedAt = now;
            await _userRepository.UpdateAsync(user);

            var products = await _productRepository.Query()
                .Where(product => product.SellerId == userId)
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsDeleted = true;
                product.UpdatedAt = now;
                await _productRepository.UpdateAsync(product);
            }
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

    }
}
