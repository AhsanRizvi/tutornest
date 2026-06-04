using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly TutorNestDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            TutorNestDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return null;
            }

            if (user.IsSuspended)
            {
                throw new UnauthorizedAccessException("Your account has been suspended by the administrator.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? ApplicationRole.Student;

            var (token, expiration) = GenerateJwtToken(user, role);

            return new AuthResponse(
                Token: token,
                Email: user.Email!,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Role: role,
                Expiration: expiration
            );
        }

        public async Task<ApplicationUser?> RegisterTeacherAsync(RegisterTeacherRequest request)
        {
            Guid? referredById = null;
            if (!string.IsNullOrEmpty(request.ReferredByCode))
            {
                var referringUser = await _context.Users.FirstOrDefaultAsync(u => u.ReferralCode == request.ReferredByCode);
                if (referringUser != null)
                {
                    referredById = referringUser.Id;
                }
            }

            var referralCode = $"REF-{request.LastName.Replace(" ", "").ToUpper()}-{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}";

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ReferralCode = referralCode,
                ReferredById = referredById,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, ApplicationRole.Teacher);
            return user;
        }

        public async Task<ApplicationUser?> RegisterStudentAsync(RegisterRequest request, Guid teacherId)
        {
            // Verify teacher exists
            var teacher = await _userManager.FindByIdAsync(teacherId.ToString());
            if (teacher == null)
            {
                throw new Exception("Teacher not found.");
            }

            // Create student user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, ApplicationRole.Student);

            // Map student to the teacher for data isolation
            var teacherStudent = new TeacherStudent
            {
                TeacherId = teacherId,
                StudentId = user.Id
            };

            _context.TeacherStudents.Add(teacherStudent);
            await _context.SaveChangesAsync();

            return user;
        }

        private (string token, DateTime expiration) GenerateJwtToken(ApplicationUser user, string role)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "TutorNestSuperSecretKeyMustBe32BytesOrLonger!";
            var issuer = _configuration["Jwt:Issuer"] ?? "TutorNestAPI";
            var audience = _configuration["Jwt:Audience"] ?? "TutorNestWeb";
            var expiryDays = double.Parse(_configuration["Jwt:ExpiryDays"] ?? "7");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, role),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            };

            var expiration = DateTime.UtcNow.AddDays(expiryDays);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
        }
    }
}
