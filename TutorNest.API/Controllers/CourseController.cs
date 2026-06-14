using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;
using TutorNest.API.Services;

namespace TutorNest.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly TutorNestDbContext _context;

        public CourseController(ICourseService courseService, TutorNestDbContext context)
        {
            _courseService = courseService;
            _context = context;
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

        private string GetUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? "Student";
        }

        [HttpPost]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                var result = await _courseService.CreateCourseAsync(request, teacherId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            try
            {
                var userId = GetUserId();
                var role = GetUserRole();

                if (role == ApplicationRole.Teacher)
                {
                    var result = await _courseService.GetTeacherCoursesAsync(userId);
                    return Ok(result);
                }
                else
                {
                    // For student, find distinct CourseIds of their enrolled classes
                    var enrolledClassIds = await _context.ClassStudents
                        .Where(cs => cs.StudentId == userId)
                        .Select(cs => cs.ClassId)
                        .ToListAsync();

                    var courses = await _context.Classes
                        .Where(c => enrolledClassIds.Contains(c.Id) && c.CourseId != null)
                        .Select(c => c.Course!)
                        .Distinct()
                        .Include(c => c.Teacher)
                        .Include(c => c.ClassGroups)
                        .Select(c => new CourseResponse(
                            c.Id,
                            c.Title,
                            c.Description,
                            c.TeacherId,
                            $"{c.Teacher.FirstName} {c.Teacher.LastName}",
                            c.ClassGroups.Count,
                            c.CreatedAt
                        ))
                        .ToListAsync();

                    return Ok(courses);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCourseById(Guid id)
        {
            try
            {
                var result = await _courseService.GetCourseByIdAsync(id);
                return Ok(result);
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

        [HttpPost("{id:guid}/classes")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> AssignClasses(Guid id, [FromBody] AssignClassesRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                await _courseService.AssignClassesToCourseAsync(id, request.ClassIds, teacherId);
                return Ok(new { message = "Classes assigned to course curriculum successfully." });
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

        [HttpGet("{id:guid}/progress")]
        public async Task<IActionResult> GetStudentProgress(Guid id)
        {
            try
            {
                var studentId = GetUserId();
                var result = await _courseService.GetStudentCourseProgressAsync(id, studentId);
                return Ok(result);
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

        [HttpGet("certificates")]
        public async Task<IActionResult> GetMyCertificates()
        {
            try
            {
                var studentId = GetUserId();
                var result = await _courseService.GetStudentCertificatesAsync(studentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("certificates/{id:guid}")]
        public async Task<IActionResult> GetCertificate(Guid id)
        {
            try
            {
                var result = await _courseService.GetCertificateAsync(id);
                return Ok(result);
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

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            try
            {
                var teacherId = GetUserId();
                await _courseService.DeleteCourseAsync(id, teacherId);
                return Ok(new { message = "Course deleted successfully." });
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
