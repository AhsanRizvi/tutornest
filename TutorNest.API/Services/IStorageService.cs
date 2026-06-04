namespace TutorNest.API.Services
{
    public interface IStorageService
    {
        /// <summary>
        /// Uploads a file and returns its public URL.
        /// </summary>
        Task<string> UploadAsync(IFormFile file, string folder = "uploads");

        /// <summary>
        /// Deletes a file by its key/path.
        /// </summary>
        Task DeleteAsync(string fileKey);
    }
}
