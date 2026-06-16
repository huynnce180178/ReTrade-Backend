using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetradeBE.Models.DTOs;
using RetradeBE.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RetradeBE.Controllers.Address
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
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
        public IActionResult GetProvinces()
        {
            var data = new[] { 
                new { ProvinceID = 201, ProvinceName = "Hà Nội" }, 
                new { ProvinceID = 202, ProvinceName = "Hồ Chí Minh" },
                new { ProvinceID = 215, ProvinceName = "Vĩnh Long" }
            };
            return Ok(data);
        }

        [AllowAnonymous]
        [HttpGet("districts")]
        public IActionResult GetDistricts([FromQuery] int provinceId)
        {
            var data = new[] { 
                new { DistrictID = 1442, DistrictName = "Quận 1" }, 
                new { DistrictID = 1443, DistrictName = "Quận 2" },
                new { DistrictID = 2034, DistrictName = "Trà Ôn" }
            };
            return Ok(data);
        }

        [AllowAnonymous]
        [HttpGet("wards")]
        public IActionResult GetWards([FromQuery] int districtId)
        {
            var data = new[] { 
                new { WardCode = "20101", WardName = "Phường 1" }, 
                new { WardCode = "20102", WardName = "Phường 2" },
                new { WardCode = "570604", WardName = "Tân Thạnh" }
            };
            return Ok(data);
        }
    }
}
