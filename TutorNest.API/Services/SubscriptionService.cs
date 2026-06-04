using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly TutorNestDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubscriptionService(TutorNestDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IEnumerable<SubscriptionPlanResponse>> GetActivePlansAsync()
        {
            return await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .Select(p => new SubscriptionPlanResponse(
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Currency,
                    p.ClassLimit,
                    p.StudentLimit,
                    p.StorageLimitBytes,
                    p.IsActive
                ))
                .ToListAsync();
        }

        public async Task<TeacherSubscriptionResponse> GetTeacherSubscriptionAsync(Guid teacherId)
        {
            var sub = await _context.TeacherSubscriptions
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.TeacherId == teacherId);

            if (sub == null)
            {
                // Fallback: Create Free subscription if somehow missing
                var freePlan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Free");
                if (freePlan == null)
                {
                    throw new InvalidOperationException("Standard 'Free' plan not found in database.");
                }

                sub = new TeacherSubscription
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId,
                    SubscriptionPlanId = freePlan.Id,
                    Status = "Active",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddYears(10),
                    StorageUsedBytes = 0,
                    PaymentProvider = "Admin",
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TeacherSubscriptions.Add(sub);
                await _context.SaveChangesAsync();
                
                // Re-fetch to load navigation properties
                sub = await _context.TeacherSubscriptions
                    .Include(s => s.SubscriptionPlan)
                    .FirstAsync(s => s.Id == sub.Id);
            }

            var classCount = await _context.Classes.CountAsync(c => c.TeacherId == teacherId);
            var studentCount = await _context.TeacherStudents.CountAsync(ts => ts.TeacherId == teacherId);

            return new TeacherSubscriptionResponse(
                sub.Id,
                sub.SubscriptionPlanId,
                sub.SubscriptionPlan.Name,
                sub.SubscriptionPlan.Price,
                sub.SubscriptionPlan.Currency,
                sub.Status,
                sub.StartDate,
                sub.EndDate,
                sub.StorageUsedBytes,
                sub.SubscriptionPlan.StorageLimitBytes,
                classCount,
                sub.SubscriptionPlan.ClassLimit,
                studentCount,
                sub.SubscriptionPlan.StudentLimit
            );
        }

        public async Task<bool> IsWithinClassLimitAsync(Guid teacherId)
        {
            var sub = await GetTeacherSubscriptionAsync(teacherId);
            return sub.ClassCount < sub.ClassLimit;
        }

        public async Task<bool> IsWithinStudentLimitAsync(Guid teacherId)
        {
            var sub = await GetTeacherSubscriptionAsync(teacherId);
            return sub.StudentCount < sub.StudentLimit;
        }

        public async Task<bool> IsWithinStorageLimitAsync(Guid teacherId, long additionalBytes)
        {
            var sub = await GetTeacherSubscriptionAsync(teacherId);
            return (sub.StorageUsedBytes + additionalBytes) <= sub.StorageLimitBytes;
        }

        public async Task TrackFileUploadAsync(Guid teacherId, Guid uploadedById, string fileName, string filePath, long fileSizeBytes)
        {
            var file = new UploadedFile
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                FilePath = filePath,
                FileSizeBytes = fileSizeBytes,
                UploadedById = uploadedById,
                TeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UploadedFiles.Add(file);

            var sub = await _context.TeacherSubscriptions.FirstOrDefaultAsync(s => s.TeacherId == teacherId);
            if (sub != null)
            {
                sub.StorageUsedBytes += fileSizeBytes;
                sub.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task TrackFileDeletionAsync(Guid teacherId, string filePath)
        {
            var file = await _context.UploadedFiles.FirstOrDefaultAsync(f => f.FilePath == filePath);
            if (file == null) return;

            _context.UploadedFiles.Remove(file);

            var sub = await _context.TeacherSubscriptions.FirstOrDefaultAsync(s => s.TeacherId == teacherId);
            if (sub != null)
            {
                sub.StorageUsedBytes = Math.Max(0, sub.StorageUsedBytes - file.FileSizeBytes);
                sub.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Try to delete physical file if path is relative/local
            try
            {
                if (filePath.StartsWith("http://localhost:5299/uploads/"))
                {
                    var fileName = Path.GetFileName(filePath);
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", fileName);
                    if (File.Exists(physicalPath))
                    {
                        File.Delete(physicalPath);
                    }
                }
            }
            catch
            {
                // Soft fail if physical file delete fails
            }
        }

        public async Task UpgradeSubscriptionAsync(Guid teacherId, Guid planId, string provider, string transactionId, string? externalSubId = null)
        {
            var sub = await _context.TeacherSubscriptions.FirstOrDefaultAsync(s => s.TeacherId == teacherId);
            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null) throw new KeyNotFoundException("Pricing plan not found.");

            if (sub == null)
            {
                sub = new TeacherSubscription
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId,
                    SubscriptionPlanId = planId,
                    Status = "Active",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    StorageUsedBytes = 0,
                    PaymentProvider = provider,
                    ExternalSubscriptionId = externalSubId,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.TeacherSubscriptions.Add(sub);
            }
            else
            {
                sub.SubscriptionPlanId = planId;
                sub.Status = "Active";
                sub.StartDate = DateTime.UtcNow;
                sub.EndDate = DateTime.UtcNow.AddMonths(1);
                sub.PaymentProvider = provider;
                sub.ExternalSubscriptionId = externalSubId;
                sub.UpdatedAt = DateTime.UtcNow;
            }

            // Write payment audit entry
            var history = new PaymentHistory
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                SubscriptionPlanId = planId,
                Amount = plan.Price,
                Currency = plan.Currency,
                Status = "Paid",
                PaymentProvider = provider,
                TransactionId = transactionId,
                PaymentDate = DateTime.UtcNow
            };

            _context.PaymentHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<PaymentHistoryResponse>> GetPaymentHistoryAsync(Guid teacherId)
        {
            return await _context.PaymentHistories
                .Where(ph => ph.TeacherId == teacherId)
                .Include(ph => ph.SubscriptionPlan)
                .OrderByDescending(ph => ph.PaymentDate)
                .Select(ph => new PaymentHistoryResponse(
                    ph.Id,
                    ph.SubscriptionPlan.Name,
                    ph.Amount,
                    ph.Currency,
                    ph.Status,
                    ph.PaymentProvider,
                    ph.TransactionId,
                    ph.PaymentDate
                ))
                .ToListAsync();
        }

        public async Task<UserProfileResponse> GetUserProfileAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new KeyNotFoundException("User profile not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Student";

            string? referralCode = user.ReferralCode;
            List<string>? referredTutors = null;

            if (role == ApplicationRole.Teacher)
            {
                referredTutors = await _context.Users
                    .Where(u => u.ReferredById == userId)
                    .Select(u => $"{u.FirstName} {u.LastName} ({u.Email})")
                    .ToListAsync();
            }

            return new UserProfileResponse(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.Bio,
                user.Subject,
                user.ProfilePictureUrl,
                role,
                referralCode,
                referredTutors
            );
        }

        public async Task UpdateUserProfileAsync(Guid userId, ProfileUpdateRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new KeyNotFoundException("User profile not found.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Bio = request.Bio;
            user.Subject = request.Subject;
            user.ProfilePictureUrl = request.ProfilePictureUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update profile: {errors}");
            }
        }

        public async Task<Guid> GetTeacherIdForUserAsync(Guid userId, string role)
        {
            if (role == ApplicationRole.Teacher)
            {
                return userId;
            }

            var teacherId = await _context.TeacherStudents
                .Where(ts => ts.StudentId == userId)
                .Select(ts => ts.TeacherId)
                .FirstOrDefaultAsync();

            if (teacherId == Guid.Empty)
            {
                throw new InvalidOperationException("Student is not mapped to any active teacher workspace.");
            }

            return teacherId;
        }
    }
}
