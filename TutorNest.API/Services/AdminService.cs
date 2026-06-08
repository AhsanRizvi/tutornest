using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISubscriptionService _subscriptionService;
        private readonly TutorNestDbContext _context;

        public AdminService(
            UserManager<ApplicationUser> userManager, 
            ISubscriptionService subscriptionService,
            TutorNestDbContext context)
        {
            _userManager = userManager;
            _subscriptionService = subscriptionService;
            _context = context;
        }

        public async Task<IEnumerable<TeacherDetailsResponse>> GetTeachersAsync()
        {
            var teachers = await _userManager.GetUsersInRoleAsync(ApplicationRole.Teacher);
            var list = new List<TeacherDetailsResponse>();

            foreach (var t in teachers)
            {
                var sub = await _subscriptionService.GetTeacherSubscriptionAsync(t.Id);
                list.Add(new TeacherDetailsResponse(
                    t.Id,
                    t.Email!,
                    t.FirstName,
                    t.LastName,
                    sub,
                    t.IsSuspended,
                    t.Theme
                ));
            }

            return list;
        }

        public async Task SuspendUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new KeyNotFoundException("User not found.");

            user.IsSuspended = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to suspend user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        public async Task UnsuspendUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new KeyNotFoundException("User not found.");

            user.IsSuspended = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to unsuspend user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        public async Task<IEnumerable<SubscriptionPlanResponse>> GetPlansAsync()
        {
            return await _context.SubscriptionPlans
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

        public async Task<SubscriptionPlanResponse> CreatePlanAsync(CreatePlanRequest request)
        {
            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Price = request.Price,
                Currency = request.Currency,
                ClassLimit = request.ClassLimit,
                StudentLimit = request.StudentLimit,
                StorageLimitBytes = request.StorageLimitBytes,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            return new SubscriptionPlanResponse(
                plan.Id,
                plan.Name,
                plan.Price,
                plan.Currency,
                plan.ClassLimit,
                plan.StudentLimit,
                plan.StorageLimitBytes,
                plan.IsActive
            );
        }

        public async Task<SubscriptionPlanResponse> UpdatePlanAsync(Guid planId, CreatePlanRequest request)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null) throw new KeyNotFoundException("Pricing plan not found.");

            plan.Name = request.Name;
            plan.Price = request.Price;
            plan.Currency = request.Currency;
            plan.ClassLimit = request.ClassLimit;
            plan.StudentLimit = request.StudentLimit;
            plan.StorageLimitBytes = request.StorageLimitBytes;
            plan.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return new SubscriptionPlanResponse(
                plan.Id,
                plan.Name,
                plan.Price,
                plan.Currency,
                plan.ClassLimit,
                plan.StudentLimit,
                plan.StorageLimitBytes,
                plan.IsActive
            );
        }

        public async Task<AdminRevenueReportResponse> GetRevenueReportAsync()
        {
            var totalRevenue = await _context.PaymentHistories
                .Where(ph => ph.Status == "Paid")
                .SumAsync(ph => ph.Amount);

            var activeSubsCount = await _context.TeacherSubscriptions
                .Where(ts => ts.Status == "Active")
                .CountAsync();

            var transactions = await _context.PaymentHistories
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

            return new AdminRevenueReportResponse(totalRevenue, activeSubsCount, transactions);
        }

        public async Task UpdateTeacherThemeAsync(Guid teacherId, string theme)
        {
            var user = await _userManager.FindByIdAsync(teacherId.ToString());
            if (user == null) throw new KeyNotFoundException("Teacher not found.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(ApplicationRole.Teacher))
            {
                throw new InvalidOperationException("User is not a teacher.");
            }

            user.Theme = theme;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to update theme: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
