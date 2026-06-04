namespace TutorNest.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            // Format a premium console message simulating an email transmission
            var border = new string('=', 60);
            var emailConsoleLog = $"\n{border}\n" +
                                  $"[MOCK EMAIL TRANSMISSION]\n" +
                                  $"To:      {toEmail}\n" +
                                  $"Subject: {subject}\n" +
                                  $"Body:\n" +
                                  $"------------------------------------------------------------\n" +
                                  $"{htmlMessage}\n" +
                                  $"------------------------------------------------------------\n" +
                                  $"{border}\n";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(emailConsoleLog);
            Console.ResetColor();

            _logger.LogInformation("Simulated email sent to {ToEmail} with subject: {Subject}", toEmail, subject);

            return Task.CompletedTask;
        }
    }
}
