using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;
using System.Security.Claims;

namespace RetradeBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuctionController : ControllerBase
    {
        private readonly IAuctionService _auctionService;

        public AuctionController(IAuctionService auctionService)
        {
            _auctionService = auctionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AuctionQueryDto query)
        {
            var result = await _auctionService.GetAuctionsAsync(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var auction = await _auctionService.GetAuctionByIdAsync(id);
            if (auction == null)
                return NotFound("Auction not found.");

            return Ok(auction);
        }

        [Authorize(Roles = nameof(RoleEnum.Seller))]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyAuctions([FromQuery] AuctionQueryDto query)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            var result = await _auctionService.GetMyAuctionsAsync(accountId, query);
            return Ok(result);
        }

        [HttpGet("my-bids")]
        public async Task<IActionResult> GetMyBids()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var result = await _auctionService.GetUserBidHistoryAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        [HttpGet("eligible-products")]
        public async Task<IActionResult> GetEligibleProducts([FromQuery] AuctionQueryDto query)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            var result = await _auctionService.GetEligibleProductsAsync(accountId, query);
            return Ok(result);
        }

        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AuctionCreateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var auction = await _auctionService.CreateAuctionAsync(accountId, dto);
                return CreatedAtAction(nameof(GetById), new { id = auction.AuctionId }, auction);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AuctionUpdateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var auction = await _auctionService.UpdateAuctionAsync(accountId, id, dto);
                return Ok(auction);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/my-deposit")]
        public async Task<IActionResult> GetMyDeposit(string id)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var deposit = await _auctionService.GetMyDepositAsync(accountId, id);
                return Ok(deposit);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/deposit/payment-url")]
        public async Task<IActionResult> CreateDepositPaymentUrl(string id, [FromBody] AuctionDepositPaymentRequestDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/Payment/vnpay-return";
                var result = await _auctionService.CreateDepositPaymentUrlAsync(accountId, id, dto, ipAddress, callbackUrl);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/bid")]
        public async Task<IActionResult> PlaceBid(string id, [FromBody] AuctionBidCreateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var result = await _auctionService.PlaceBidAsync(accountId, id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        [HttpPost("{id}/end")]
        public async Task<IActionResult> EndAuction(string id)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var result = await _auctionService.EndAuctionAsync(accountId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = $"{nameof(RoleEnum.Seller)},{nameof(RoleEnum.Admin)}")]
        [HttpPost("{id}/relist")]
        public async Task<IActionResult> RelistAuction(string id, [FromBody] AuctionUpdateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId)) return Unauthorized();

            try
            {
                var result = await _auctionService.RelistAuctionAsync(accountId, id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
