using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RetradeBE.Data;
using RetradeBE.Hubs;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class AuctionService : IAuctionService
    {
        private static readonly string[] VisibleStatuses = { "Upcoming", "Ongoing" };
        private const decimal MinimumDepositAmount = 20000m;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IHubContext<AuctionHub> _auctionHub;

        public AuctionService(
            IAuctionRepository auctionRepository,
            IAccountRepository accountRepository,
            AppDbContext context,
            IPaymentService paymentService,
            IHubContext<AuctionHub> auctionHub)
        {
            _auctionRepository = auctionRepository;
            _accountRepository = accountRepository;
            _context = context;
            _paymentService = paymentService;
            _auctionHub = auctionHub;
        }

        public async Task<PagedResultDto<AuctionListDto>> GetAuctionsAsync(AuctionQueryDto query)
        {
            var now = GetAuctionNow();
            var auctions = ApplyAuctionFilters(_auctionRepository.Query(), query);

            if (!query.IncludeEnded)
            {
                auctions = auctions.Where(a => a.EndTime == null || a.EndTime > now);
                auctions = auctions.Where(a => a.Status == null || VisibleStatuses.Contains(a.Status));
            }

            return await PageAuctionsAsync(auctions, query);
        }

        public async Task<PagedResultDto<AuctionListDto>> GetMyAuctionsAsync(string accountId, AuctionQueryDto query)
        {
            var account = await GetAccountAsync(accountId);
            var userId = account.UserId ?? throw new Exception("Account is not linked to a user.");

            query.IncludeEnded = true;
            var auctions = ApplyAuctionFilters(_auctionRepository.Query().Where(a => a.SellerId == userId), query);
            return await PageAuctionsAsync(auctions, query);
        }

        public async Task<AuctionDetailDto?> GetAuctionByIdAsync(string auctionId)
        {
            var auction = await _auctionRepository.GetByIdAsync(auctionId);
            return auction == null ? null : MapToDetailDto(auction);
        }

        public async Task<PagedResultDto<ProductListDto>> GetEligibleProductsAsync(string accountId, AuctionQueryDto query)
        {
            var account = await GetAccountAsync(accountId);
            var roles = await _accountRepository.GetRolesAsync(accountId);
            var isAdmin = HasRole(roles, "Admin");
            var userId = account.UserId ?? throw new Exception("Account is not linked to a user.");

            var products = _auctionRepository.QueryEligibleProducts();
            if (!isAdmin)
            {
                products = products.Where(p => p.SellerId == userId);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.Trim().ToLower();
                products = products.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(search)) ||
                    (p.Seller != null && ((p.Seller.FirstName ?? "") + " " + (p.Seller.LastName ?? "")).ToLower().Contains(search)));
            }

            var totalItems = await products.CountAsync();
            var pageSize = NormalizePageSize(query.PageSize);
            var page = Math.Max(1, query.Page);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));

            var items = await products
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                    SellerName = p.Seller != null ? $"{p.Seller.FirstName} {p.Seller.LastName}".Trim() : null,
                    MainImageUrl = p.ProductImage.Where(pi => pi.IsMain == true).Select(pi => pi.Image.ImageUrl).FirstOrDefault()
                        ?? p.ProductImage.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();

            return new PagedResultDto<ProductListDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        public async Task<AuctionDetailDto> CreateAuctionAsync(string accountId, AuctionCreateDto dto)
        {
            var account = await GetAccountAsync(accountId);
            var roles = await _accountRepository.GetRolesAsync(accountId);
            var isAdmin = HasRole(roles, "Admin");
            var userId = account.UserId ?? throw new Exception("Account is not linked to a user.");

            if (string.IsNullOrWhiteSpace(dto.ProductId))
                throw new Exception("ProductId is required.");
            ValidateAuctionValues(dto.StartingPrice, dto.MinIncrement, dto.BuyNowPrice, dto.StartTime, dto.EndTime);

            var product = await _auctionRepository.QueryEligibleProducts()
                .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId);

            if (product == null)
                throw new Exception("This product is not ready for auction or already has an open auction.");

            if (!isAdmin && product.SellerId != userId)
                throw new Exception("You can only create auctions for your own products.");

            if (await _auctionRepository.HasOpenAuctionForProductAsync(dto.ProductId))
                throw new Exception("This product already has an open auction.");

            var now = GetAuctionNow();
            var auction = new RetradeBE.Models.Auction
            {
                AuctionId = await GenerateAuctionIdAsync(),
                ProductId = product.ProductId,
                SellerId = product.SellerId,
                StartingPrice = dto.StartingPrice,
                CurrentPrice = dto.StartingPrice,
                MinIncrement = dto.MinIncrement,
                BuyNowPrice = dto.BuyNowPrice,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = dto.StartTime > now ? "Upcoming" : "Ongoing",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _auctionRepository.AddAsync(auction);
            var saved = await _auctionRepository.GetByIdAsync(auction.AuctionId);
            var result = MapToDetailDto(saved!);
            await NotifyAuctionChangedAsync(result, "AuctionCreated");
            return result;
        }

        public async Task<AuctionDetailDto> UpdateAuctionAsync(string accountId, string auctionId, AuctionUpdateDto dto)
        {
            var account = await GetAccountAsync(accountId);
            var roles = await _accountRepository.GetRolesAsync(accountId);
            var isAdmin = HasRole(roles, "Admin");
            var userId = account.UserId ?? throw new Exception("Account is not linked to a user.");

            ValidateAuctionValues(dto.StartingPrice, dto.MinIncrement, dto.BuyNowPrice, dto.StartTime, dto.EndTime);

            var auction = await _auctionRepository.GetByIdAsync(auctionId);
            if (auction == null)
                throw new Exception("Auction not found.");

            if (!isAdmin && auction.SellerId != userId)
                throw new Exception("You can only update your own auctions.");

            var now = GetAuctionNow();
            if (auction.StartTime.HasValue && auction.StartTime.Value <= now)
                throw new Exception("Auction can only be updated before it becomes active.");

            if (auction.EndTime.HasValue && auction.EndTime.Value <= now)
                throw new Exception("Ended auctions cannot be updated.");

            if (auction.Bid.Any())
                throw new Exception("Auction with existing bids cannot be updated.");

            if (dto.StartTime <= now)
                throw new Exception("Auction start time must remain in the future.");

            auction.StartingPrice = dto.StartingPrice;
            auction.CurrentPrice = dto.StartingPrice;
            auction.MinIncrement = dto.MinIncrement;
            auction.BuyNowPrice = dto.BuyNowPrice;
            auction.StartTime = dto.StartTime;
            auction.EndTime = dto.EndTime;
            auction.Status = "Upcoming";
            auction.UpdatedAt = now;

            await _auctionRepository.UpdateAsync(auction);
            var saved = await _auctionRepository.GetByIdAsync(auction.AuctionId);
            var result = MapToDetailDto(saved!);
            await NotifyAuctionChangedAsync(result, "AuctionUpdated");
            return result;
        }

        public async Task<AuctionDepositDto?> GetMyDepositAsync(string accountId, string auctionId)
        {
            var account = await GetAccountAsync(accountId);
            var userId = account.UserId ?? throw new Exception("Account is not linked to a user.");

            var deposit = await _context.AuctionDeposit
                .AsNoTracking()
                .Where(x => x.AuctionId == auctionId && x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            return deposit == null ? null : await MapDepositDtoAsync(deposit);
        }

        public async Task<CreateVnPayPaymentResponseDto> CreateDepositPaymentUrlAsync(
            string accountId,
            string auctionId,
            AuctionDepositPaymentRequestDto dto,
            string ipAddress)
        {
            var account = await GetAccountAsync(accountId);
            var userId = account.UserId ?? throw new Exception("Account is not linked to a user.");

            if (dto.DepositAmount < MinimumDepositAmount)
                throw new Exception("Deposit amount must be at least 20,000 VND.");
            if (!dto.PolicyAccepted)
                throw new Exception("Auction policy must be accepted before paying deposit.");

            var auction = await _auctionRepository.GetByIdAsync(auctionId);
            if (auction == null)
                throw new Exception("Auction not found.");
            if (auction.SellerId == userId)
                throw new Exception("You cannot deposit for your own auction.");

            var status = ResolveStatus(auction);
            if (IsTerminalStatus(status))
                throw new Exception("This auction is not available for deposit.");

            var deposit = await _context.AuctionDeposit
                .Where(x => x.AuctionId == auctionId && x.UserId == userId && (x.Status == "Pending" || x.Status == "Paid"))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (deposit == null)
            {
                deposit = new AuctionDeposit
                {
                    AuctionDepositId = $"ADEP_{Guid.NewGuid():N}",
                    AuctionId = auctionId,
                    UserId = userId,
                    CreatedAt = GetAuctionNow(),
                    Status = "Pending"
                };
                _context.AuctionDeposit.Add(deposit);
            }

            if (deposit.Status != "Paid")
            {
                deposit.DepositAmount = dto.DepositAmount;
            }
            deposit.PolicyAccepted = dto.PolicyAccepted;
            await _context.SaveChangesAsync();

            return await _paymentService.CreateVnPayPaymentUrlAsync(accountId, new CreateVnPayPaymentRequestDto
            {
                AuctionDepositId = deposit.AuctionDepositId,
                Amount = dto.DepositAmount,
                OrderDescription = $"Auction deposit for {auction.Product?.Name ?? auction.AuctionId}",
                BankCode = dto.BankCode,
                Locale = dto.Locale
            }, ipAddress);
        }

        public async Task<AuctionBidResultDto> PlaceBidAsync(string accountId, string auctionId, AuctionBidCreateDto dto)
        {
            var account = await GetAccountAsync(accountId);
            var userId = account.UserId ?? throw new Exception("Account is not linked to a user.");
            if (dto.BidAmount <= 0)
                throw new Exception("Bid amount must be greater than 0.");

            var auction = await _auctionRepository.GetByIdAsync(auctionId);
            if (auction == null)
                throw new Exception("Auction not found.");
            if (auction.SellerId == userId)
                throw new Exception("You cannot bid on your own auction.");
            if (ResolveStatus(auction) != "Ongoing")
                throw new Exception("Bids can only be placed on active auctions.");

            var deposit = await _context.AuctionDeposit
                .Where(x => x.AuctionId == auctionId && x.UserId == userId && x.Status == "Paid")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
            if (deposit == null || deposit.PolicyAccepted != true)
                throw new Exception("A paid deposit and accepted policy are required before bidding.");
            var spentBidAmount = await GetSpentBidAmountAsync(auctionId, userId);
            var maxBidAmount = Math.Max(0, (deposit.DepositAmount ?? 0) - MinimumDepositAmount - spentBidAmount);
            var isBuyNowBid = auction.BuyNowPrice.HasValue && dto.BidAmount == auction.BuyNowPrice.Value;
            if (dto.BidAmount > maxBidAmount)
                throw new Exception($"Bid amount cannot exceed your bidding limit (deposit - {MinimumDepositAmount:N0} VND).");

            var currentPrice = GetCurrentPrice(auction);
            var minimumBid = GetMinimumNextBid(auction);
            if (dto.BidAmount <= currentPrice)
                throw new Exception("Bid amount must be greater than the current bid.");
            if (!isBuyNowBid && dto.BidAmount < minimumBid)
                throw new Exception($"Bid amount must be at least {minimumBid:N0} VND.");
            if (auction.BuyNowPrice.HasValue && dto.BidAmount > auction.BuyNowPrice.Value)
                throw new Exception("Bid amount cannot be greater than the buy now price.");

            var outbidUserIds = auction.Bid
                .Where(b => b.Status == "Highest" && !string.IsNullOrWhiteSpace(b.UserId))
                .Select(b => b.UserId!)
                .Distinct()
                .ToList();

            foreach (var bid in auction.Bid.Where(b => b.Status == "Highest"))
            {
                bid.Status = "Outbid";
            }

            var newBid = new Bid
            {
                BidId = $"BID_{Guid.NewGuid():N}",
                AuctionId = auction.AuctionId,
                UserId = userId,
                BidAmount = dto.BidAmount,
                Status = "Highest",
                CreatedAt = GetAuctionNow()
            };

            _context.Bid.Add(newBid);
            auction.CurrentPrice = dto.BidAmount;
            auction.UpdatedAt = GetAuctionNow();

            var endedByBuyNow = isBuyNowBid;
            string? orderId = null;

            if (endedByBuyNow)
            {
                orderId = await CompleteAuctionAsync(auction, newBid, "EndedByBuyNow");
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            var saved = await _auctionRepository.GetByIdAsync(auction.AuctionId);
            var mapped = MapToDetailDto(saved!);
            await NotifyAuctionChangedAsync(mapped, endedByBuyNow ? "AuctionEndedByBuyNow" : "BidPlaced");
            await NotifyAuctionDepositChangedAsync(deposit, endedByBuyNow ? "DepositAppliedToOrder" : "BidPlaced");

            foreach (var outbidUserId in outbidUserIds.Where(id => id != userId))
            {
                var outbidDeposit = await _context.AuctionDeposit
                    .Where(d => d.AuctionId == auctionId && d.UserId == outbidUserId && d.Status == "Paid")
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefaultAsync();

                if (outbidDeposit != null)
                {
                    await NotifyAuctionDepositChangedAsync(outbidDeposit, "BidReleased");
                }
            }

            return new AuctionBidResultDto
            {
                Auction = mapped,
                Deposit = await MapDepositDtoAsync(deposit),
                AuctionEnded = endedByBuyNow,
                OrderId = orderId,
                Message = endedByBuyNow ? "Bid matched buy now price. Auction ended." : "Bid placed successfully."
            };
        }

        public async Task<int> ProcessDueAuctionsAsync(CancellationToken cancellationToken = default)
        {
            var now = GetAuctionNow();
            var dueAuctionIds = await _context.Auction
                .Where(a => (a.Status == "Ongoing" || a.Status == "Upcoming")
                    && a.EndTime.HasValue
                    && a.EndTime.Value <= now)
                .Select(a => a.AuctionId)
                .ToListAsync(cancellationToken);

            var processed = 0;
            foreach (var auctionId in dueAuctionIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var auction = await _auctionRepository.GetByIdAsync(auctionId);
                if (auction == null || auction.Status is "EndedByTime" or "EndedByBuyNow" or "Cancelled")
                    continue;

                var highestBid = auction.Bid
                    .Where(b => b.BidAmount.HasValue)
                    .OrderByDescending(b => b.BidAmount)
                    .ThenBy(b => b.CreatedAt)
                    .FirstOrDefault();

                if (highestBid == null)
                {
                    auction.Status = "EndedNoBid";
                    auction.UpdatedAt = now;
                    await CreateRefundsForLosersAsync(auction, null);
                    await _context.SaveChangesAsync(cancellationToken);
                    var saved = await _auctionRepository.GetByIdAsync(auction.AuctionId);
                    if (saved != null)
                    {
                        await NotifyAuctionChangedAsync(MapToDetailDto(saved), "AuctionEndedNoBid");
                    }
                }
                else
                {
                    await CompleteAuctionAsync(auction, highestBid, "EndedByTime");
                    var saved = await _auctionRepository.GetByIdAsync(auction.AuctionId);
                    if (saved != null)
                    {
                        await NotifyAuctionChangedAsync(MapToDetailDto(saved), "AuctionEndedByTime");
                    }
                }

                processed++;
            }

            return processed;
        }

        private IQueryable<RetradeBE.Models.Auction> ApplyAuctionFilters(IQueryable<RetradeBE.Models.Auction> auctions, AuctionQueryDto query)
        {
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.Trim().ToLower();
                auctions = auctions.Where(a =>
                    (a.Product != null && a.Product.Name != null && a.Product.Name.ToLower().Contains(search)) ||
                    (a.Seller != null && ((a.Seller.FirstName ?? "") + " " + (a.Seller.LastName ?? "")).ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && !string.Equals(query.Status, "All", StringComparison.OrdinalIgnoreCase))
            {
                var now = GetAuctionNow();
                auctions = query.Status switch
                {
                    "Upcoming" => auctions.Where(a => a.StartTime != null && a.StartTime > now && (a.EndTime == null || a.EndTime > now) && a.Status != "Cancelled"),
                    "Ongoing" => auctions.Where(a => (a.StartTime == null || a.StartTime <= now) && (a.EndTime == null || a.EndTime > now) && a.Status != "Cancelled"),
                    "Ended" => auctions.Where(a =>
                        (a.EndTime != null && a.EndTime <= now)
                        || a.Status == "Ended"
                        || a.Status == "EndedByBuyNow"
                        || a.Status == "EndedByTime"
                        || a.Status == "EndedNoBid"),
                    _ => auctions.Where(a => a.Status == query.Status)
                };
            }

            if (!string.IsNullOrWhiteSpace(query.SellerId))
            {
                auctions = auctions.Where(a => a.SellerId == query.SellerId);
            }

            return auctions;
        }

        private async Task<PagedResultDto<AuctionListDto>> PageAuctionsAsync(IQueryable<RetradeBE.Models.Auction> auctions, AuctionQueryDto query)
        {
            var totalItems = await auctions.CountAsync();
            var pageSize = NormalizePageSize(query.PageSize);
            var page = Math.Max(1, query.Page);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));

            auctions = query.SortBy?.ToLower() switch
            {
                "ending_soon" => auctions.OrderBy(a => a.EndTime),
                "starting_soon" => auctions.OrderBy(a => a.StartTime),
                "price_desc" => auctions.OrderByDescending(a => a.CurrentPrice),
                "price_asc" => auctions.OrderBy(a => a.CurrentPrice),
                "oldest" => auctions.OrderBy(a => a.CreatedAt),
                _ => auctions.OrderByDescending(a => a.CreatedAt)
            };

            var entities = await auctions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<AuctionListDto>
            {
                Items = entities.Select(MapToListDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        private decimal GetCurrentPrice(RetradeBE.Models.Auction auction)
        {
            return auction.Bid
                .Where(b => b.BidAmount.HasValue)
                .OrderByDescending(b => b.BidAmount)
                .Select(b => b.BidAmount!.Value)
                .FirstOrDefault(auction.CurrentPrice ?? auction.StartingPrice ?? 0);
        }

        private decimal GetMinimumNextBid(RetradeBE.Models.Auction auction)
        {
            var hasBid = auction.Bid.Any(b => b.BidAmount.HasValue);
            if (!hasBid)
            {
                return auction.StartingPrice ?? 0;
            }

            return GetCurrentPrice(auction) + (auction.MinIncrement ?? 0);
        }

        private async Task<AuctionDepositDto> MapDepositDtoAsync(AuctionDeposit deposit)
        {
            var spentBidAmount = await GetSpentBidAmountAsync(deposit.AuctionId, deposit.UserId);
            return MapDepositDto(deposit, spentBidAmount);
        }

        private async Task<decimal> GetSpentBidAmountAsync(string? auctionId, string? userId)
        {
            if (string.IsNullOrWhiteSpace(auctionId) || string.IsNullOrWhiteSpace(userId))
            {
                return 0;
            }

            return await _context.Bid
                .AsNoTracking()
                .Where(b => b.AuctionId == auctionId && b.UserId == userId)
                .SumAsync(b => b.BidAmount ?? 0);
        }

        private AuctionDepositDto MapDepositDto(AuctionDeposit deposit, decimal spentBidAmount = 0)
        {
            var paid = deposit.Status == "Paid" && deposit.PolicyAccepted == true;
            var totalDepositAmount = deposit.DepositAmount ?? 0;
            var availableDepositAmount = Math.Max(0, totalDepositAmount - spentBidAmount);
            return new AuctionDepositDto
            {
                AuctionDepositId = deposit.AuctionDepositId,
                AuctionId = deposit.AuctionId,
                UserId = deposit.UserId,
                DepositAmount = availableDepositAmount,
                TotalDepositAmount = deposit.DepositAmount,
                HeldBidAmount = spentBidAmount,
                PolicyAccepted = deposit.PolicyAccepted == true,
                Status = deposit.Status,
                CreatedAt = deposit.CreatedAt,
                MaxBidAmount = Math.Max(0, availableDepositAmount - MinimumDepositAmount),
                CanBid = paid
            };
        }

        private async Task<string?> CompleteAuctionAsync(RetradeBE.Models.Auction auction, Bid winnerBid, string status)
        {
            if (string.IsNullOrWhiteSpace(winnerBid.UserId) || !winnerBid.BidAmount.HasValue)
                return null;

            var existingOrder = await _context.Order.FirstOrDefaultAsync(o => o.AuctionId == auction.AuctionId);
            if (existingOrder != null)
            {
                return existingOrder.OrderId;
            }

            var now = GetAuctionNow();
            var winningAmount = winnerBid.BidAmount.Value;
            var winnerDeposit = await _context.AuctionDeposit
                .Where(d => d.AuctionId == auction.AuctionId && d.UserId == winnerBid.UserId && d.Status == "Paid")
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            if (winnerDeposit == null)
                throw new Exception("Winner deposit not found.");

            auction.Status = status;
            auction.WinnerId = winnerBid.UserId;
            auction.CurrentPrice = winningAmount;
            auction.UpdatedAt = now;

            foreach (var bid in auction.Bid)
            {
                bid.Status = bid.BidId == winnerBid.BidId ? "Winning" : "Lost";
            }

            if (auction.Product != null)
            {
                auction.Product.Status = ProductStatusEnum.Sold.ToString();
                auction.Product.StockQuantity = 0;
                auction.Product.UpdatedAt = now;
            }

            winnerDeposit.Status = "AppliedToOrder";

            var appliedDeposit = Math.Min(winnerDeposit.DepositAmount ?? 0, winningAmount);
            var finalAmount = Math.Max(0, winningAmount - appliedDeposit);
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                OrderCode = GenerateOrderCode(),
                UserId = winnerBid.UserId,
                SellerId = auction.SellerId,
                ProductId = auction.ProductId,
                AuctionId = auction.AuctionId,
                Quantity = 1,
                UnitPrice = winningAmount,
                TotalAmount = winningAmount,
                DiscountAmount = appliedDeposit,
                ShippingFee = 0,
                FinalAmount = finalAmount,
                AddressSnapshot = await GetDefaultAddressSnapshotAsync(winnerBid.UserId),
                Status = OrderStatusEnum.Pending.ToString(),
                CreatedAt = now,
                UpdatedAt = now,
                ShippingProvider = "Seller Arrangement",
                ExpectedDeliveryTime = now.AddDays(5)
            };
            _context.Order.Add(order);

            var depositPayment = new Payment
            {
                PaymentId = $"PAY_AUC_{Guid.NewGuid():N}",
                OrderId = order.OrderId,
                UserId = winnerBid.UserId,
                Amount = appliedDeposit,
                PaymentMethod = "AUCTION_DEPOSIT",
                ProviderTransactionId = winnerDeposit.AuctionDepositId,
                Status = "Success",
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Payment.Add(depositPayment);

            await CreateRefundsForLosersAsync(auction, winnerBid.UserId);
            await CreateWinnerRemainderRefundAsync(auction, winnerDeposit, winningAmount);
            await _context.SaveChangesAsync();

            return order.OrderId;
        }

        private async Task CreateRefundsForLosersAsync(RetradeBE.Models.Auction auction, string? winnerId)
        {
            var deposits = await _context.AuctionDeposit
                .Where(d => d.AuctionId == auction.AuctionId && d.Status == "Paid" && d.UserId != winnerId)
                .ToListAsync();

            foreach (var deposit in deposits)
            {
                var refundAmount = Math.Max(0, (deposit.DepositAmount ?? 0) - MinimumDepositAmount);
                deposit.Status = refundAmount > 0 ? "RefundPending" : "Refunded";

                if (refundAmount <= 0 || string.IsNullOrWhiteSpace(deposit.UserId))
                    continue;

                var note = $"Auction refund for {auction.AuctionId}. Fee {MinimumDepositAmount:N0} VND retained.";
                var exists = await _context.RefundRequest.AnyAsync(r =>
                    r.UserId == deposit.UserId
                    && r.Amount == refundAmount
                    && r.Note == note
                    && r.Status != "Rejected");
                if (exists) continue;

                _context.RefundRequest.Add(new RefundRequest
                {
                    RefundRequestId = $"REF_{Guid.NewGuid():N}",
                    UserId = deposit.UserId,
                    Amount = refundAmount,
                    Note = note,
                    Status = "Pending",
                    RequestedAt = GetAuctionNow(),
                    CreatedAt = GetAuctionNow(),
                    UpdatedAt = GetAuctionNow()
                });

            }
        }

        private async Task CreateWinnerRemainderRefundAsync(RetradeBE.Models.Auction auction, AuctionDeposit winnerDeposit, decimal winningAmount)
        {
            var remainder = (winnerDeposit.DepositAmount ?? 0) - winningAmount - MinimumDepositAmount;
            if (remainder <= 0 || string.IsNullOrWhiteSpace(winnerDeposit.UserId))
                return;

            var note = $"Auction winner remaining deposit for {auction.AuctionId}.";
            var exists = await _context.RefundRequest.AnyAsync(r =>
                r.UserId == winnerDeposit.UserId
                && r.Amount == remainder
                && r.Note == note
                && r.Status != "Rejected");
            if (exists) return;

            _context.RefundRequest.Add(new RefundRequest
            {
                RefundRequestId = $"REF_{Guid.NewGuid():N}",
                UserId = winnerDeposit.UserId,
                Amount = remainder,
                Note = note,
                Status = "Pending",
                RequestedAt = GetAuctionNow(),
                CreatedAt = GetAuctionNow(),
                UpdatedAt = GetAuctionNow()
            });

        }

        private async Task<string?> GetDefaultAddressSnapshotAsync(string userId)
        {
            var address = await _context.Address
                .Where(a => a.UserId == userId && a.IsDeleted != true)
                .OrderByDescending(a => a.IsDefault == true)
                .ThenByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (address == null)
                return null;

            return $"{address.ReceiverName ?? ""} - {address.ReceiverPhone ?? ""} - {address.Street ?? ""}, {address.WardCode ?? ""}, {address.DistrictId?.ToString() ?? ""}, {address.ProvinceId?.ToString() ?? ""}";
        }

        private static string GenerateOrderCode()
        {
            var now = GetAuctionNow();
            var random = Random.Shared.Next(10, 99);
            return $"ORD{random}{now:yyyyMMddHHmm}";
        }

        private AuctionListDto MapToListDto(RetradeBE.Models.Auction auction)
        {
            var highestBid = auction.Bid
                .Where(b => b.BidAmount.HasValue)
                .OrderByDescending(b => b.BidAmount)
                .Select(b => b.BidAmount)
                .FirstOrDefault();

            return new AuctionListDto
            {
                AuctionId = auction.AuctionId,
                ProductId = auction.ProductId,
                ProductName = auction.Product?.Name,
                ProductImageUrl = GetMainImageUrl(auction.Product),
                CategoryName = auction.Product?.Category?.Name,
                Condition = auction.Product?.Condition,
                SellerId = auction.SellerId,
                SellerName = GetUserName(auction.Seller),
                StartingPrice = auction.StartingPrice,
                CurrentPrice = highestBid ?? auction.CurrentPrice ?? auction.StartingPrice,
                HighestBid = highestBid,
                MinIncrement = auction.MinIncrement,
                BuyNowPrice = auction.BuyNowPrice,
                StartTime = auction.StartTime,
                EndTime = auction.EndTime,
                Status = ResolveStatus(auction),
                BidCount = auction.Bid.Count,
                CreatedAt = auction.CreatedAt
            };
        }

        private AuctionDetailDto MapToDetailDto(RetradeBE.Models.Auction auction)
        {
            var dto = new AuctionDetailDto();
            var list = MapToListDto(auction);

            dto.AuctionId = list.AuctionId;
            dto.ProductId = list.ProductId;
            dto.ProductName = list.ProductName;
            dto.ProductImageUrl = list.ProductImageUrl;
            dto.CategoryName = list.CategoryName;
            dto.Condition = list.Condition;
            dto.SellerId = list.SellerId;
            dto.SellerName = list.SellerName;
            dto.StartingPrice = list.StartingPrice;
            dto.CurrentPrice = list.CurrentPrice;
            dto.HighestBid = list.HighestBid;
            dto.MinIncrement = list.MinIncrement;
            dto.BuyNowPrice = list.BuyNowPrice;
            dto.StartTime = list.StartTime;
            dto.EndTime = list.EndTime;
            dto.Status = list.Status;
            dto.BidCount = list.BidCount;
            dto.CreatedAt = list.CreatedAt;

            dto.ProductDescription = auction.Product?.Description;
            dto.StockQuantity = auction.Product?.StockQuantity;
            dto.WeightGram = auction.Product?.WeightGram;
            dto.LengthCm = auction.Product?.LengthCm;
            dto.WidthCm = auction.Product?.WidthCm;
            dto.HeightCm = auction.Product?.HeightCm;
            dto.WinnerId = auction.WinnerId;
            dto.WinnerName = GetUserName(auction.Winner);
            dto.Images = auction.Product?.ProductImage
                .OrderBy(pi => pi.SortOrder)
                .Select(pi => new ProductImageDto
                {
                    ImageId = pi.ImageId,
                    ImageUrl = pi.Image?.ImageUrl,
                    AltText = pi.Image?.AltText,
                    IsMain = pi.IsMain,
                    SortOrder = pi.SortOrder
                })
                .ToList() ?? new List<ProductImageDto>();
            dto.Attributes = auction.Product?.ProductAttribute
                .Where(pa => pa.IsDeleted != true)
                .Select(pa => new ProductAttributeValueDto
                {
                    AttributeId = pa.AttributeId,
                    AttributeName = pa.Attribute?.Name,
                    Value = pa.Value,
                    DataType = pa.Attribute?.DataType,
                    Unit = pa.Attribute?.Unit
                })
                .ToList() ?? new List<ProductAttributeValueDto>();
            dto.RecentBids = auction.Bid
                .OrderByDescending(b => b.CreatedAt)
                .Take(8)
                .Select(b => new AuctionBidSummaryDto
                {
                    BidId = b.BidId,
                    UserId = b.UserId,
                    BidderName = GetUserName(b.User),
                    BidAmount = b.BidAmount,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToList();

            return dto;
        }

        public async Task<List<UserBidHistoryDto>> GetUserBidHistoryAsync(string accountId)
        {
            var account = await GetAccountAsync(accountId);
            var userId = account.UserId ?? throw new Exception("Account is not linked to a user.");

            var bids = await _context.Bid
                .AsNoTracking()
                .Include(b => b.Auction)
                    .ThenInclude(a => a!.Product)
                        .ThenInclude(p => p!.ProductImage)
                            .ThenInclude(pi => pi.Image)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bids.Select(b =>
            {
                var auctionStatus = b.Auction != null ? ResolveStatus(b.Auction) ?? string.Empty : string.Empty;
                return new UserBidHistoryDto
                {
                    BidId = b.BidId,
                    BidAmount = b.BidAmount ?? 0,
                    BidStatus = ResolveUserBidHistoryStatus(b.Status, auctionStatus),
                    CreatedAt = b.CreatedAt ?? DateTime.MinValue,
                    AuctionId = b.AuctionId ?? string.Empty,
                    AuctionStatus = auctionStatus,
                    ProductId = b.Auction?.ProductId ?? string.Empty,
                    ProductName = b.Auction?.Product?.Name ?? string.Empty,
                    ProductImageUrl = b.Auction?.Product != null ? GetMainImageUrl(b.Auction.Product) ?? string.Empty : string.Empty,
                    CurrentPrice = b.Auction?.CurrentPrice ?? b.Auction?.StartingPrice ?? 0,
                    EndTime = b.Auction?.EndTime ?? DateTime.MinValue
                };
            }).ToList();
        }

        private static void ValidateAuctionValues(
            decimal startingPrice,
            decimal minIncrement,
            decimal? buyNowPrice,
            DateTime startTime,
            DateTime endTime)
        {
            if (startingPrice <= 0)
                throw new Exception("Starting bid must be greater than 0.");
            if (minIncrement <= 0)
                throw new Exception("Bid step must be greater than 0.");
            if (endTime <= startTime)
                throw new Exception("Auction end time must be after start time.");
            if (!buyNowPrice.HasValue)
                throw new Exception("Buy now price is required.");
            if (buyNowPrice.Value <= startingPrice)
                throw new Exception("Buy now price must be greater than starting bid.");
        }

        private async Task NotifyAuctionChangedAsync(AuctionDetailDto auction, string eventType)
        {
            if (string.IsNullOrWhiteSpace(auction.AuctionId))
            {
                return;
            }

            var payload = new
            {
                EventType = eventType,
                Auction = auction
            };

            await _auctionHub.Clients
                .Group(AuctionHub.AuctionListGroupName)
                .SendAsync("AuctionListChanged", payload);

            await _auctionHub.Clients
                .Group(AuctionHub.GetAuctionGroupName(auction.AuctionId))
                .SendAsync("AuctionUpdated", payload);

            if (!string.IsNullOrWhiteSpace(auction.SellerId))
            {
                await _auctionHub.Clients
                    .Group(AuctionHub.GetSellerAuctionGroupName(auction.SellerId))
                    .SendAsync("SellerAuctionChanged", payload);
            }
        }

        private async Task NotifyAuctionDepositChangedAsync(AuctionDeposit deposit, string eventType)
        {
            if (string.IsNullOrWhiteSpace(deposit.AuctionId) || string.IsNullOrWhiteSpace(deposit.UserId))
            {
                return;
            }

            await _auctionHub.Clients
                .Group(AuctionHub.GetAuctionUserGroupName(deposit.AuctionId, deposit.UserId))
                .SendAsync("AuctionDepositChanged", new
                {
                    EventType = eventType,
                    Deposit = await MapDepositDtoAsync(deposit)
                });
        }

        private static string ResolveUserBidHistoryStatus(string? bidStatus, string? auctionStatus)
        {
            var normalizedBidStatus = (bidStatus ?? string.Empty).ToLowerInvariant();
            var normalizedAuctionStatus = (auctionStatus ?? string.Empty).ToLowerInvariant();
            var isTerminal = normalizedAuctionStatus is "ended" or "endedbybuynow" or "endedbytime" or "endednobid";

            if (normalizedAuctionStatus == "cancelled")
                return "Cancelled";

            if (isTerminal)
            {
                if (normalizedBidStatus is "highest" or "winning" or "won")
                    return "Won";
                if (normalizedBidStatus is "outbid" or "lost")
                    return "Lost";
            }

            if (normalizedAuctionStatus == "ongoing")
            {
                if (normalizedBidStatus is "highest" or "winning" or "won")
                    return "Winning";
                if (normalizedBidStatus is "outbid" or "lost")
                    return "Outbid";
            }

            return bidStatus ?? string.Empty;
        }

        private async Task<Account> GetAccountAsync(string accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Account does not exist.");
            return account;
        }

        private async Task<string> GenerateAuctionIdAsync()
        {
            return await Task.FromResult($"AUC_{Guid.NewGuid():N}");
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize <= 0) return 12;
            return Math.Min(pageSize, 100);
        }

        private static bool HasRole(IEnumerable<string> roles, string roleName)
        {
            return roles.Any(role => string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));
        }

        private static string? GetUserName(User? user)
        {
            if (user == null) return null;
            var name = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? user.Email : name;
        }

        private static string? GetMainImageUrl(Product? product)
        {
            return product?.ProductImage
                .Where(pi => pi.IsMain == true)
                .Select(pi => pi.Image?.ImageUrl)
                .FirstOrDefault()
                ?? product?.ProductImage
                    .OrderBy(pi => pi.SortOrder)
                    .Select(pi => pi.Image?.ImageUrl)
                    .FirstOrDefault();
        }

        private static string? ResolveStatus(RetradeBE.Models.Auction auction)
        {
            if (auction.Status == "Cancelled") return auction.Status;
            if (auction.Status == "EndedByBuyNow" || auction.Status == "EndedByTime" || auction.Status == "EndedNoBid") return auction.Status;
            var now = GetAuctionNow();
            if (auction.EndTime.HasValue && auction.EndTime.Value <= now) return "Ended";
            if (auction.StartTime.HasValue && auction.StartTime.Value > now) return "Upcoming";
            return "Ongoing";
        }

        private static DateTime GetAuctionNow()
        {
            return DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);
        }


        private static bool IsTerminalStatus(string? status)
        {
            return status is "Ended" or "EndedByBuyNow" or "EndedByTime" or "EndedNoBid" or "Cancelled";
        }
    }
}
