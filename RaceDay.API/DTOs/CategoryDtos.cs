namespace RaceDay.API.DTOs;

public record CategoryRequest(string CategoryName, string CategoryType, string ActivityType, decimal? Distance, int? MinimumAge, int? MaximumAge, string? Description);
