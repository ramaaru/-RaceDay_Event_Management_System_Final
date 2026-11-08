namespace RaceDay.API.Models;

public class Weather
{
    public int WeatherID { get; set; }
    public int EventID { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public decimal? Temperature { get; set; }
    public string? WeatherCondition { get; set; }
    public decimal? Humidity { get; set; }
    public decimal? WindSpeed { get; set; }
    public decimal? RainProbability { get; set; }
    public Event? Event { get; set; }
}
