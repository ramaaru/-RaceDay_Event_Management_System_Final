namespace RaceDay.API.DTOs;

public record ResultRequest(int EnrolmentID, TimeSpan? FinishTime, int? Position, int? CategoryPosition, decimal? Pace, string ResultStatus);
