using AutoMapper;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Repositories;

namespace RetradeBE.Services
{
    public class ReviewService : IReviewService
    {
        private const string CompletedStatus = "Completed";

        private readonly IOrderRepository _orderRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;

        public ReviewService(
            IOrderRepository orderRepository,
            IReviewRepository reviewRepository,
            IMapper mapper)
        {
            _orderRepository = orderRepository;
            _reviewRepository = reviewRepository;
            _mapper = mapper;
        }

        public async Task<ReviewResponseDto?> GetByBuyerOrderAsync(string buyerId, string orderId)
        {
            if (string.IsNullOrWhiteSpace(buyerId) || string.IsNullOrWhiteSpace(orderId))
            {
                return null;
            }

            var review = await _reviewRepository.GetByBuyerOrderAsync(buyerId, orderId);
            return review == null ? null : _mapper.Map<ReviewResponseDto>(review);
        }

        public async Task<ReviewResponseDto?> CreateAsync(string buyerId, ReviewCreateDto request)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return null;
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
            if (order == null || order.UserId != buyerId)
            {
                return null;
            }

            if (!string.Equals(order.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Review can only be submitted after the order is completed.");
            }

            var existingReview = await _reviewRepository.GetByBuyerOrderAsync(buyerId, request.OrderId);
            if (existingReview != null)
            {
                throw new InvalidOperationException("You have already reviewed this order.");
            }

            var review = new Review
            {
                ReviewId = Guid.NewGuid().ToString("N"),
                ReviewerId = buyerId,
                SellerId = order.SellerId,
                OrderId = order.OrderId,
                Rating = request.Rating,
                Comment = request.Comment?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(review);
            return _mapper.Map<ReviewResponseDto>(review);
        }
    }
}
