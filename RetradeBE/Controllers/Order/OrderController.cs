using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;
using System.Security.Claims;

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
        public async Task<IActionResult> GetMyOrders([FromQuery] OrderSearchQueryDto query)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            return Ok(await _orderService.GetMyOrdersAsync(accountId, query));
        }

        [HttpGet("seller-orders")]
        public async Task<IActionResult> GetSellerOrders([FromQuery] OrderSearchQueryDto query)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            return Ok(await _orderService.GetSellerOrdersAsync(accountId, query));
        }

        [HttpGet("admin")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderSearchQueryDto query)
        {
            return Ok(await _orderService.GetAllOrdersAsync(query));
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderDetail(string orderId)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            var order = await _orderService.GetOrderDetailAsync(accountId, orderId);
            if (order == null) return NotFound("Order not found.");

            return Ok(order);
        }

        [HttpPatch("{orderId}/confirm")]
        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        public async Task<IActionResult> ConfirmOrder(string orderId)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var order = await _orderService.ConfirmOrderAsync(accountId, orderId);
                if (order == null) return NotFound("Order not found.");

                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{orderId}/status")]
        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        public async Task<IActionResult> UpdateStatus(string orderId, OrderStatusUpdateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var order = await _orderService.UpdateStatusAsync(accountId, orderId, dto);
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
