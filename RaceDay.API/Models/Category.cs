namespace RaceDay.API.Models;

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public decimal? Distance { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public string? Description { get; set; }
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}
