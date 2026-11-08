namespace RaceDay.API.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? StartLocation { get; set; }
    public string? FinishLocation { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? BannerBlobName { get; set; }
    public string? BannerContentType { get; set; }
    public User? Organiser { get; set; }
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public Route? Route { get; set; }
    public ICollection<Weather> WeatherRecords { get; set; } = new List<Weather>();
}
