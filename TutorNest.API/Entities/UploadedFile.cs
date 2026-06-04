using System;

namespace TutorNest.API.Entities
{
    public class UploadedFile
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        public Guid UploadedById { get; set; }
        public ApplicationUser UploadedBy { get; set; } = null!;

        public Guid TeacherId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
