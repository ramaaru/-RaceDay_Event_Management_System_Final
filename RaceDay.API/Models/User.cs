using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class User
{
    public int UserID { get; set; }
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [Required, MaxLength(100), EmailAddress] public string Email { get; set; } = "";
    [Required, MaxLength(255)] public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Participant";
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ProfileImageBlobName { get; set; }
    public string? ProfileImageContentType { get; set; }
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<EventEnrolment> EventEnrolments { get; set; } = new List<EventEnrolment>();
}
