using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Entities;

namespace TutorNest.API.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            TutorNestDbContext context, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration)
        {
            // Ensure Database is created and migrated
            await context.Database.MigrateAsync();

            // 1. Seed Roles
            var roles = new[] { ApplicationRole.Admin, ApplicationRole.Teacher, ApplicationRole.Student };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                }
            }

            // 2. Seed Default Admin
            var adminEmail = configuration["AdminSettings:Email"] ?? "admin@tutornest.com";
            var adminPassword = configuration["AdminSettings:Password"] ?? "Admin@123";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, ApplicationRole.Admin);
                }
                else
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to seed default admin user: {errors}");
                }
            }

            // 3. Seed Subscription Plans
            if (!await context.SubscriptionPlans.AnyAsync())
            {
                var plans = new List<SubscriptionPlan>
                {
                    new SubscriptionPlan
                    {
                        Id = Guid.NewGuid(),
                        Name = "Free",
                        Price = 0.00m,
                        Currency = "USD",
                        ClassLimit = 2,
                        StudentLimit = 5,
                        StorageLimitBytes = 100L * 1024 * 1024, // 100 MB
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = Guid.NewGuid(),
                        Name = "Basic",
                        Price = 5.00m,
                        Currency = "USD",
                        ClassLimit = 10,
                        StudentLimit = 25,
                        StorageLimitBytes = 2L * 1024 * 1024 * 1024, // 2 GB
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = Guid.NewGuid(),
                        Name = "Pro",
                        Price = 15.00m,
                        Currency = "USD",
                        ClassLimit = 9999,
                        StudentLimit = 9999,
                        StorageLimitBytes = 10L * 1024 * 1024 * 1024, // 10 GB
                        IsActive = true
                    }
                };

                await context.SubscriptionPlans.AddRangeAsync(plans);
                await context.SaveChangesAsync();
            }

            // 4. Link existing Teachers to the Free Plan
            var freePlan = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Free");
            if (freePlan != null)
            {
                var teachers = await userManager.GetUsersInRoleAsync(ApplicationRole.Teacher);
                foreach (var teacher in teachers)
                {
                    var hasSub = await context.TeacherSubscriptions.AnyAsync(s => s.TeacherId == teacher.Id);
                    if (!hasSub)
                    {
                        var newSub = new TeacherSubscription
                        {
                            Id = Guid.NewGuid(),
                            TeacherId = teacher.Id,
                            SubscriptionPlanId = freePlan.Id,
                            Status = "Active",
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddYears(10),
                            StorageUsedBytes = 0,
                            PaymentProvider = "Admin",
                            UpdatedAt = DateTime.UtcNow
                        };
                        await context.TeacherSubscriptions.AddAsync(newSub);
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
