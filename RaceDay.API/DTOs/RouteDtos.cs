namespace RaceDay.API.DTOs;

public record RouteRequest(string RouteName, decimal Distance, decimal? ElevationGain, string? DifficultyLevel, string? StartPoint, string? EndPoint, string? MapURL, string? Description);
