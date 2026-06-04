using Microsoft.AspNetCore.Identity;

namespace TutorNest.API.Entities
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public const string Admin = "Admin";
        public const string Teacher = "Teacher";
        public const string Student = "Student";
    }
}
