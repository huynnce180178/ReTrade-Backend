using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.Enums;
using RetradeBE.Services;
using System.Security.Claims;

namespace RetradeBE.Controllers.Report
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost("review/{reviewId}")]
        public async Task<IActionResult> ReportReview(string reviewId, [FromBody] ReportCreateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Unauthorized();
            }

            try
            {
                var report = await _reportService.ReportReviewAsync(accountId, reviewId, request);
                return CreatedAtAction(nameof(GetById), new { reportId = report.ReportId }, report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("buyer/{orderId}")]
        public async Task<IActionResult> ReportBuyer(string orderId, [FromBody] ReportCreateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Unauthorized();
            }

            try
            {
                var report = await _reportService.ReportBuyerAsync(accountId, orderId, request);
                return CreatedAtAction(nameof(GetById), new { reportId = report.ReportId }, report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("seller/{orderId}")]
        public async Task<IActionResult> ReportSeller(string orderId, [FromBody] ReportCreateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Unauthorized();
            }

            try
            {
                var report = await _reportService.ReportSellerAsync(accountId, orderId, request);
                return CreatedAtAction(nameof(GetById), new { reportId = report.ReportId }, report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("product/{productId}")]
        public async Task<IActionResult> ReportProduct(string productId, [FromBody] ReportCreateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Unauthorized();
            }

            try
            {
                var report = await _reportService.ReportProductAsync(accountId, productId, request);
                return CreatedAtAction(nameof(GetById), new { reportId = report.ReportId }, report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var reports = await _reportService.GetAllAsync();
            return Ok(reports);
        }

        [HttpGet("{reportId}")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> GetById(string reportId)
        {
            var report = await _reportService.GetByIdAsync(reportId);
            if (report == null)
            {
                return NotFound("Report not found.");
            }

            return Ok(report);
        }

        [HttpPatch("{reportId}/status")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> UpdateStatus(string reportId, [FromBody] ReportStatusUpdateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var report = await _reportService.UpdateStatusAsync(reportId, request);
                if (report == null)
                {
                    return NotFound("Report not found.");
                }

                return Ok(report);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("flagged-users")]
        [Authorize(Roles = nameof(RoleEnum.Admin))]
        public async Task<IActionResult> GetFlaggedUsers()
        {
            var users = await _reportService.GetFlaggedUsersAsync();
            return Ok(users);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Unauthorized();
            }

            var history = await _reportService.GetHistoryAsync(accountId);
            return Ok(history);
        }
    }
}
