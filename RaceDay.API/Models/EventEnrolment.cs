namespace RaceDay.API.Models;

public class EventEnrolment
{
    public int EnrolmentID { get; set; }
    public int UserID { get; set; }
    public int EventCategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string? RaceNumber { get; set; }
    public string EnrolmentStatus { get; set; } = "Pending";
    public User? User { get; set; }
    public EventCategory? EventCategory { get; set; }
    public Result? Result { get; set; }
}
