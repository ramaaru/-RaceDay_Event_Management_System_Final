using Microsoft.AspNetCore.Identity;
using RaceDay.API.Models;

namespace RaceDay.API.Services;

public class PasswordService
{
    private readonly PasswordHasher<User> _hasher = new();
    public string Hash(User user, string password) => _hasher.HashPassword(user, password);
    public bool Verify(User user, string hash, string password) =>
        _hasher.VerifyHashedPassword(user, hash, password) != PasswordVerificationResult.Failed;
}
