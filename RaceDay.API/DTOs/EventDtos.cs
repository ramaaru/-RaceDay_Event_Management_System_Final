namespace RaceDay.API.DTOs;

public record EventCreateRequest(string EventName, string? Description, DateTime EventDate, TimeSpan? StartTime, string Location, decimal Distance, string EventType, string? StartLocation, string? FinishLocation);
public record EventUpdateRequest(string EventName, string? Description, DateTime EventDate, TimeSpan? StartTime, string Location, decimal Distance, string EventType, string? StartLocation, string? FinishLocation, string Status);
public record EventResponse(int EventID, int OrganiserID, string EventName, string? Description, DateTime EventDate, TimeSpan? StartTime, string Location, decimal Distance, string EventType, string? StartLocation, string? FinishLocation, string Status, DateTime CreatedAt, bool HasBanner);
