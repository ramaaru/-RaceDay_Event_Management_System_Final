using System.Collections.Concurrent;
using RaceDay.API.Models;

namespace RaceDay.API.Services;

// Simple shared in-memory store used by tests to share entities across test server instances.
public static class SharedStore
{
    public static readonly ConcurrentDictionary<string, User> UsersByEmail = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<int, Event> Events = new();
    public static readonly ConcurrentDictionary<int, Category> Categories = new();
    public static readonly ConcurrentDictionary<int, EventCategory> EventCategories = new();
    private static int _nextEventCategoryId = 1000;
    private static int _nextEnrolmentId = 1000;

    public static int GetNextEventCategoryId() => Interlocked.Increment(ref _nextEventCategoryId);
    public static int GetNextEnrolmentId() => Interlocked.Increment(ref _nextEnrolmentId);
}
