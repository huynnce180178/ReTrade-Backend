using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;
using RetradeBE.Services.Offer;

namespace RetradeBE.Controllers.Offer
{
    [Route("api/seller-offers")]
    [ApiController]
    [Authorize(Roles = nameof(RoleEnum.Seller))]
    public class SellerOfferController : ControllerBase
    {
        private readonly IOfferService _offerService;
        private readonly IAccountService _accountService;

        public SellerOfferController(IOfferService offerService, IAccountService accountService)
        {
            _offerService = offerService;
            _accountService = accountService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOffersBySeller()
        {
            try
            {
                var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountId))
                    return Unauthorized();
                var account = await _accountService.GetByIdAsync(accountId);
                var sellerId = account.UserId;
                var offers = await _offerService.GetOffersBySellerAsync(sellerId);

                return Ok(offers);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("counter-offer")]
        public async Task<IActionResult> CounterOffer(
            [FromBody] CounterOfferDto request)
        {
            try
            {
                var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountId))
                    return Unauthorized();
                var account = await _accountService.GetByIdAsync(accountId);
                var sellerId = account.UserId;
                var offer = await _offerService
                    .CounterOfferAsync(sellerId, request);

                return Ok(offer);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{offerId}/response")]
        public async Task<IActionResult> RespondToOffer(
            string offerId,
            [FromBody] RespondToOfferDto request)
        {
            try
            {
                var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountId))
                    return Unauthorized();
                var account = await _accountService.GetByIdAsync(accountId);
                var sellerId = account.UserId;
                if (string.IsNullOrEmpty(sellerId))
                    return Unauthorized();
                var offer = await _offerService
                    .RespondToOfferAsync(sellerId, offerId, request.Accept!.Value);

                return Ok(offer);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpPatch("{offerId}/cancel")]
        //public async Task<IActionResult> CancelOffer(string offerId)
        //{
        //    try
        //    {
        //        var buyerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //        if (string.IsNullOrEmpty(buyerUserId))
        //            return Unauthorized();

        //        var offer = await _offerService
        //            .CancelOfferAsync(buyerUserId, offerId);

        //        return Ok(offer);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}
    }


}
