namespace RaceDay.API.Models;

public class Route
{
    public int RouteID { get; set; }
    public int EventID { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public decimal? ElevationGain { get; set; }
    public string? DifficultyLevel { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
    public string? MapURL { get; set; }
    public string? Description { get; set; }
    public Event? Event { get; set; }
}
