namespace RaceDay.API.DTOs;

public record WeatherRequest(decimal? Temperature, string? WeatherCondition, decimal? Humidity, decimal? WindSpeed, decimal? RainProbability);
