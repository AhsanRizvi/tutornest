using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Entities;

namespace TutorNest.API.Data
{
    public class TutorNestDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public TutorNestDbContext(DbContextOptions<TutorNestDbContext> options) : base(options)
        {
        }

        public DbSet<TeacherStudent> TeacherStudents { get; set; } = null!;
        public DbSet<Class> Classes { get; set; } = null!;
        public DbSet<ClassStudent> ClassStudents { get; set; } = null!;
        public DbSet<Video> Videos { get; set; } = null!;
        public DbSet<ClassVideo> ClassVideos { get; set; } = null!;
        public DbSet<VideoProgress> VideoProgresses { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; } = null!;
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; } = null!;
        public DbSet<Announcement> Announcements { get; set; } = null!;
        public DbSet<AnnouncementRead> AnnouncementReads { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<TeacherSubscription> TeacherSubscriptions { get; set; } = null!;
        public DbSet<PaymentHistory> PaymentHistories { get; set; } = null!;
        public DbSet<UploadedFile> UploadedFiles { get; set; } = null!;
        public DbSet<LiveClass> LiveClasses { get; set; } = null!;
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Certificate> Certificates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. TeacherStudent configuration
            builder.Entity<TeacherStudent>()
                .HasKey(ts => new { ts.TeacherId, ts.StudentId });

            builder.Entity<TeacherStudent>()
                .HasOne(ts => ts.Teacher)
                .WithMany(u => u.TeacherStudents)
                .HasForeignKey(ts => ts.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeacherStudent>()
                .HasOne(ts => ts.Student)
                .WithMany()
                .HasForeignKey(ts => ts.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Class configuration
            builder.Entity<Class>()
                .HasOne(c => c.Teacher)
                .WithMany(u => u.CreatedClasses)
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Class>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<Class>()
                .Property(c => c.Description)
                .HasMaxLength(500);

            // 3. ClassStudent configuration
            builder.Entity<ClassStudent>()
                .HasKey(cs => new { cs.ClassId, cs.StudentId });

            builder.Entity<ClassStudent>()
                .HasOne(cs => cs.Class)
                .WithMany(c => c.EnrolledStudents)
                .HasForeignKey(cs => cs.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ClassStudent>()
                .HasOne(cs => cs.Student)
                .WithMany(u => u.EnrolledClasses)
                .HasForeignKey(cs => cs.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // 4. Video configuration
            builder.Entity<Video>()
                .HasOne(v => v.Teacher)
                .WithMany(u => u.UploadedVideos)
                .HasForeignKey(v => v.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Video>()
                .Property(v => v.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Entity<Video>()
                .Property(v => v.Description)
                .HasMaxLength(500);

            builder.Entity<Video>()
                .Property(v => v.VideoUrl)
                .IsRequired()
                .HasMaxLength(500);

            // 5. ClassVideo configuration
            builder.Entity<ClassVideo>()
                .HasKey(cv => new { cv.ClassId, cv.VideoId });

            builder.Entity<ClassVideo>()
                .HasOne(cv => cv.Class)
                .WithMany(c => c.AssignedVideos)
                .HasForeignKey(cv => cv.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ClassVideo>()
                .HasOne(cv => cv.Video)
                .WithMany(v => v.AssignedClasses)
                .HasForeignKey(cv => cv.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            // 6. VideoProgress configuration
            builder.Entity<VideoProgress>()
                .HasKey(vp => new { vp.StudentId, vp.VideoId });

            builder.Entity<VideoProgress>()
                .HasOne(vp => vp.Student)
                .WithMany(u => u.VideoProgresses)
                .HasForeignKey(vp => vp.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VideoProgress>()
                .HasOne(vp => vp.Video)
                .WithMany(v => v.Progresses)
                .HasForeignKey(vp => vp.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            // 7. Assignment configurations
            builder.Entity<Assignment>()
                .HasOne(a => a.Class)
                .WithMany()
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Assignment>()
                .Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Entity<Assignment>()
                .Property(a => a.Description)
                .IsRequired();

            builder.Entity<Assignment>()
                .Property(a => a.Type)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Assignment>()
                .Property(a => a.ConfigJson)
                .HasMaxLength(2000);

            // 8. AssignmentSubmission configurations
            builder.Entity<AssignmentSubmission>()
                .HasOne(asb => asb.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(asb => asb.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AssignmentSubmission>()
                .HasOne(asb => asb.Student)
                .WithMany()
                .HasForeignKey(asb => asb.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AssignmentSubmission>()
                .Property(asb => asb.AnswerText)
                .HasMaxLength(4000);

            builder.Entity<AssignmentSubmission>()
                .Property(asb => asb.AttachmentUrl)
                .HasMaxLength(500);

            builder.Entity<AssignmentSubmission>()
                .Property(asb => asb.Feedback)
                .HasMaxLength(1000);

            // 9. Announcement configurations
            builder.Entity<Announcement>()
                .HasOne(ann => ann.Teacher)
                .WithMany()
                .HasForeignKey(ann => ann.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Announcement>()
                .HasOne(ann => ann.Class)
                .WithMany()
                .HasForeignKey(ann => ann.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Announcement>()
                .Property(ann => ann.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Entity<Announcement>()
                .Property(ann => ann.Content)
                .IsRequired();

            builder.Entity<Announcement>()
                .Property(ann => ann.AttachmentUrl)
                .HasMaxLength(500);

            // 10. AnnouncementRead configurations
            builder.Entity<AnnouncementRead>()
                .HasKey(ar => new { ar.StudentId, ar.AnnouncementId });

            builder.Entity<AnnouncementRead>()
                .HasOne(ar => ar.Student)
                .WithMany()
                .HasForeignKey(ar => ar.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AnnouncementRead>()
                .HasOne(ar => ar.Announcement)
                .WithMany()
                .HasForeignKey(ar => ar.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            // 11. Notification configurations
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
                .Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(500);

            builder.Entity<Notification>()
                .Property(n => n.Type)
                .IsRequired()
                .HasMaxLength(50);

            // 12. ApplicationUser configurations
            builder.Entity<ApplicationUser>()
                .Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<ApplicationUser>()
                .Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            // 13. SubscriptionPlan configurations
            builder.Entity<SubscriptionPlan>()
                .Property(sp => sp.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<SubscriptionPlan>()
                .Property(sp => sp.Currency)
                .IsRequired()
                .HasMaxLength(10);

            builder.Entity<SubscriptionPlan>()
                .Property(sp => sp.Price)
                .HasPrecision(18, 2);

            // 14. TeacherSubscription configurations
            builder.Entity<TeacherSubscription>()
                .HasOne(ts => ts.Teacher)
                .WithMany()
                .HasForeignKey(ts => ts.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeacherSubscription>()
                .HasOne(ts => ts.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(ts => ts.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeacherSubscription>()
                .Property(ts => ts.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<TeacherSubscription>()
                .Property(ts => ts.PaymentProvider)
                .HasMaxLength(50);

            builder.Entity<TeacherSubscription>()
                .Property(ts => ts.ExternalSubscriptionId)
                .HasMaxLength(150);

            // 15. PaymentHistory configurations
            builder.Entity<PaymentHistory>()
                .HasOne(ph => ph.Teacher)
                .WithMany()
                .HasForeignKey(ph => ph.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PaymentHistory>()
                .HasOne(ph => ph.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(ph => ph.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PaymentHistory>()
                .Property(ph => ph.Currency)
                .IsRequired()
                .HasMaxLength(10);

            builder.Entity<PaymentHistory>()
                .Property(ph => ph.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<PaymentHistory>()
                .Property(ph => ph.PaymentProvider)
                .HasMaxLength(50);

            builder.Entity<PaymentHistory>()
                .Property(ph => ph.TransactionId)
                .HasMaxLength(150);

            builder.Entity<PaymentHistory>()
                .Property(ph => ph.Amount)
                .HasPrecision(18, 2);

            // 16. UploadedFile configurations
            builder.Entity<UploadedFile>()
                .HasOne(uf => uf.UploadedBy)
                .WithMany()
                .HasForeignKey(uf => uf.UploadedById)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UploadedFile>()
                .Property(uf => uf.FileName)
                .IsRequired()
                .HasMaxLength(250);

            builder.Entity<UploadedFile>()
                .Property(uf => uf.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            // 17. LiveClass configurations
            builder.Entity<LiveClass>()
                .HasOne(lc => lc.Class)
                .WithMany(c => c.LiveClasses)
                .HasForeignKey(lc => lc.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<LiveClass>()
                .HasOne(lc => lc.Teacher)
                .WithMany()
                .HasForeignKey(lc => lc.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<LiveClass>()
                .Property(lc => lc.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Entity<LiveClass>()
                .Property(lc => lc.Description)
                .HasMaxLength(500);

            builder.Entity<LiveClass>()
                .Property(lc => lc.MeetingLink)
                .IsRequired()
                .HasMaxLength(500);

            builder.Entity<LiveClass>()
                .Property(lc => lc.RecordingUrl)
                .HasMaxLength(500);

            // 18. Course configurations
            builder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Course>()
                .Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Entity<Course>()
                .Property(c => c.Description)
                .HasMaxLength(500);

            // 19. Certificate configurations
            builder.Entity<Certificate>()
                .HasOne(ct => ct.Student)
                .WithMany(u => u.Certificates)
                .HasForeignKey(ct => ct.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Certificate>()
                .HasOne(ct => ct.Course)
                .WithMany(c => c.Certificates)
                .HasForeignKey(ct => ct.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Certificate>()
                .HasOne(ct => ct.Class)
                .WithMany(c => c.Certificates)
                .HasForeignKey(ct => ct.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Certificate>()
                .Property(ct => ct.CertificateCode)
                .IsRequired()
                .HasMaxLength(100);

            builder.Entity<Certificate>()
                .HasIndex(ct => ct.CertificateCode)
                .IsUnique();

            // 20. ApplicationUser extensions
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.ReferredBy)
                .WithMany(u => u.ReferredTeachers)
                .HasForeignKey(u => u.ReferredById)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ApplicationUser>()
                .Property(u => u.ReferralCode)
                .HasMaxLength(50);

            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.ReferralCode)
                .IsUnique();
        }
    }
}
