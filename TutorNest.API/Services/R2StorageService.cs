using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace TutorNest.API.Services
{
    public class R2StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _publicUrl;

        private static readonly string[] AllowedExtensions =
            { ".pdf", ".jpg", ".jpeg", ".png", ".zip", ".doc", ".docx", ".txt", ".mp4", ".mov", ".webm", ".mkv" };

        public R2StorageService(IConfiguration config)
        {
            var accountId = config["R2:AccountId"];
            var accessKey = config["R2:AccessKeyId"];
            var secretKey = config["R2:SecretAccessKey"];

            if (string.IsNullOrWhiteSpace(accountId)) accountId = "dummy-account-id";
            if (string.IsNullOrWhiteSpace(accessKey)) accessKey = "dummy-access-key";
            if (string.IsNullOrWhiteSpace(secretKey)) secretKey = "dummy-secret-key";

            _bucketName = config["R2:BucketName"] ?? "tutornest-uploads";
            _publicUrl = config["R2:PublicUrl"]?.TrimEnd('/') ?? $"https://{_bucketName}.{accountId}.r2.cloudflarestorage.com";

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,   // Required for R2
                SignatureVersion = "4"
            };

            _s3Client = new AmazonS3Client(credentials, s3Config);
        }

        public async Task<string> UploadAsync(IFormFile file, string folder = "uploads")
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"File type '{ext}' is not allowed.");

            var uniqueKey = $"{folder}/{Guid.NewGuid()}{ext}";

            using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = uniqueKey,
                InputStream = stream,
                ContentType = GetContentType(ext),
                // Make the file publicly readable
                CannedACL = S3CannedACL.PublicRead,
                DisablePayloadSigning = true // Required for R2 chunked upload support
            };

            await _s3Client.PutObjectAsync(request);

            return $"{_publicUrl}/{uniqueKey}";
        }

        public async Task DeleteAsync(string fileKey)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };
            await _s3Client.DeleteObjectAsync(request);
        }

        private static string GetContentType(string ext) => ext switch
        {
            ".pdf"  => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"  => "image/png",
            ".zip"  => "application/zip",
            ".doc"  => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt"  => "text/plain",
            ".mp4"  => "video/mp4",
            ".mov"  => "video/quicktime",
            ".webm" => "video/webm",
            ".mkv"  => "video/x-matroska",
            _       => "application/octet-stream"
        };
    }
}
