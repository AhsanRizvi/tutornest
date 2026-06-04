namespace TutorNest.API.DTOs
{
    public record LoginRequest(string Email, string Password);

    public record RegisterRequest(string Email, string Password, string FirstName, string LastName);

    public record RegisterTeacherRequest(string Email, string Password, string FirstName, string LastName, string? ReferredByCode = null);

    public record AuthResponse(
        string Token, 
        string Email, 
        string FirstName, 
        string LastName, 
        string Role, 
        DateTime Expiration
    );
}
