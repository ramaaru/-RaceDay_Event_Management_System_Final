namespace RaceDay.API.DTOs;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string? PhoneNumber, string Role);
public record LoginRequest(string Email, string Password);
public record AuthResponse(int UserID, string FirstName, string LastName, string Email, string Role);
public record ProfileResponse(int UserID, string FirstName, string LastName, string Email, string? PhoneNumber, string Role, DateTime CreatedAt);
public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);
