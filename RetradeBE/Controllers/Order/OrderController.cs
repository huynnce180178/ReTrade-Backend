using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;

namespace RetradeBE.Controllers.Order
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders([FromQuery] string userId, [FromQuery] OrderSearchQueryDto query)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("UserId is required.");

            return Ok(await _orderService.GetMyOrdersAsync(userId, query));
        }

        [HttpGet("seller-orders")]
        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        public async Task<IActionResult> GetSellerOrders([FromQuery] string sellerId, [FromQuery] OrderSearchQueryDto query)
        {
            if (string.IsNullOrWhiteSpace(sellerId)) return BadRequest("SellerId is required.");

            return Ok(await _orderService.GetSellerOrdersAsync(sellerId, query));
        }

        [HttpGet("seller-orders/odata")]
        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        [EnableQuery(PageSize = 20, MaxTop = 100)]
        public IActionResult GetSellerOrdersOData([FromQuery] string sellerId)
        {
            if (string.IsNullOrWhiteSpace(sellerId)) return BadRequest("SellerId is required.");

            return Ok(_orderService.QuerySellerOrders(sellerId));
        }

        [HttpGet("seller-orders/statistics")]
        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        public async Task<IActionResult> GetSellerSalesStatistics([FromQuery] string sellerId, [FromQuery] int periodDays = 30)
        {
            if (string.IsNullOrWhiteSpace(sellerId)) return BadRequest("SellerId is required.");

            return Ok(await _orderService.GetSellerSalesStatisticsAsync(sellerId, periodDays));
        }

        [HttpGet("admin")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderSearchQueryDto query)
        {
            return Ok(await _orderService.GetAllOrdersAsync(query));
        }

        [HttpGet("{orderId}")]
        [Authorize(Roles = nameof(RoleEnum.Seller))]
        public async Task<IActionResult> GetOrderDetail(string orderId, [FromQuery] string sellerId)
        {
            if (string.IsNullOrWhiteSpace(sellerId)) return BadRequest("SellerId is required.");

            var order = await _orderService.GetOrderDetailAsync(sellerId, orderId);
            if (order == null) return NotFound("Order not found.");

            return Ok(order);
        }

        [HttpPatch("{orderId}/confirm")]
        [Authorize(Roles = nameof(RoleEnum.Seller))]
        public async Task<IActionResult> ConfirmOrder(string orderId, [FromQuery] string sellerId)
        {
            if (string.IsNullOrWhiteSpace(sellerId)) return BadRequest("SellerId is required.");

            try
            {
                var order = await _orderService.ConfirmOrderAsync(sellerId, orderId);
                if (order == null) return NotFound("Order not found.");

                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{orderId}/status")]
        [Authorize(Roles = nameof(RoleEnum.Seller))]
        public async Task<IActionResult> UpdateStatus(string orderId, [FromQuery] string sellerId, OrderStatusUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(sellerId)) return BadRequest("SellerId is required.");

            try
            {
                var order = await _orderService.UpdateStatusAsync(sellerId, orderId, dto);
                if (order == null) return NotFound("Order not found.");

                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
