using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using RetradeBE.Services.Ghn;

namespace RetradeBE.Controllers.Address
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;
        private readonly IGhnService _ghnService;

        public AddressController(IAddressService addressService, IGhnService ghnService)
        {
            _addressService = addressService;
            _ghnService = ghnService;
        }

        [HttpGet("my-addresses")]
        public async Task<IActionResult> GetMyAddresses()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            return Ok(await _addressService.GetMyAddressesAsync(accountId));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddressCreateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var address = await _addressService.CreateAsync(accountId, dto);
            if (address == null) return NotFound("User account not found.");

            return Ok(address);
        }

        [HttpPut("{addressId}")]
        public async Task<IActionResult> Update(string addressId, [FromBody] AddressUpdateDto dto)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var address = await _addressService.UpdateAsync(accountId, addressId, dto);
            if (address == null) return NotFound("Address not found.");

            return Ok(address);
        }

        [HttpPatch("{addressId}/set-default")]
        public async Task<IActionResult> SetDefault(string addressId)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var address = await _addressService.SetDefaultAsync(accountId, addressId);
            if (address == null) return NotFound("Address not found.");

            return Ok(address);
        }

        [HttpDelete("{addressId}")]
        public async Task<IActionResult> Delete(string addressId)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountId)) return Unauthorized();

            var deleted = await _addressService.DeleteAsync(accountId, addressId);
            if (!deleted) return NotFound("Address not found.");

            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            return Ok(await _ghnService.GetProvincesAsync());
        }

        [AllowAnonymous]
        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] int provinceId)
        {
            return Ok(await _ghnService.GetDistrictsAsync(provinceId));
        }

        [AllowAnonymous]
        [HttpGet("wards")]
        public async Task<IActionResult> GetWards([FromQuery] int districtId)
        {
            return Ok(await _ghnService.GetWardsAsync(districtId));
        }
    }
}
