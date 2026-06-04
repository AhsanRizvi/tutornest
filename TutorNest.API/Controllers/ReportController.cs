using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorNest.API.Entities;
using TutorNest.API.Services;

namespace TutorNest.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated correctly.");
            }
            return userId;
        }

        [HttpGet("class/{classId:guid}/pdf")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> ExportClassProgressPdf(Guid classId)
        {
            try
            {
                var teacherId = GetUserId();
                var pdfBytes = await _reportService.GenerateClassProgressReportAsync(classId, teacherId);
                return File(pdfBytes, "application/pdf", $"class_progress_{classId}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("assignment/{assignmentId:guid}/pdf")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> ExportAssignmentResultsPdf(Guid assignmentId)
        {
            try
            {
                var teacherId = GetUserId();
                var pdfBytes = await _reportService.GenerateAssignmentResultsReportAsync(assignmentId, teacherId);
                return File(pdfBytes, "application/pdf", $"assignment_results_{assignmentId}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("admin/platform/pdf")]
        [Authorize(Roles = ApplicationRole.Admin)]
        public async Task<IActionResult> ExportAdminPlatformPdf()
        {
            try
            {
                var pdfBytes = await _reportService.GenerateAdminPlatformReportAsync();
                return File(pdfBytes, "application/pdf", "platform_overview_report.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("admin/revenue/pdf")]
        [Authorize(Roles = ApplicationRole.Admin)]
        public async Task<IActionResult> ExportAdminRevenuePdf()
        {
            try
            {
                var pdfBytes = await _reportService.GenerateAdminRevenueReportAsync();
                return File(pdfBytes, "application/pdf", "admin_revenue_report.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("certificate/{id:guid}/pdf")]
        public async Task<IActionResult> ExportCertificatePdf(Guid id)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateCertificatePdfAsync(id);
                return File(pdfBytes, "application/pdf", $"certificate_{id}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
