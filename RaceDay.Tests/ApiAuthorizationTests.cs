using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.Tests;

public class RaceDayWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<RaceDayDbContext>();
            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase("RaceDayTests-" + Guid.NewGuid()));
        });
    }
}

public class ApiAuthorizationTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public ApiAuthorizationTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task UnauthenticatedUser_CannotCreateEvent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var response = await client.PostAsJsonAsync("/api/events", new EventCreateRequest(
            "Unauthorized Event", "Test", DateTime.UtcNow.AddDays(10), null, "Polokwane", 10, "Run", null, null));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidLogin_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "Test", "Participant", "invalid-login@test.com", "Password123!", "0711111111", "Participant"));
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            "invalid-login@test.com", "WrongPassword!"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_IsRejected_WhenCreatingEvent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "Peter", "Participant", "participant-role@test.com", "Password123!", "0711111112", "Participant"));
        var login = await LoginWithSessionAsync(client, new LoginRequest(
            "participant-role@test.com", "Password123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await client.PostAsJsonAsync("/api/events", new EventCreateRequest(
            "Participant Event", "Should fail", DateTime.UtcNow.AddDays(5), null, "Polokwane", 10, "Run", null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "Olivia", "Organiser", "organiser-create@test.com", "Password123!", "0711111113", "Organiser"));
        var login = await LoginWithSessionAsync(client, new LoginRequest(
            "organiser-create@test.com", "Password123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await client.PostAsJsonAsync("/api/events", new EventCreateRequest(
            "Organiser Event", "Created by organiser", DateTime.UtcNow.AddDays(5), null, "Polokwane", 10, "Run", null, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanEnroll_AndEnrollmentIsRecorded()
    {
        using var organiser = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await organiser.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "Oscar", "Organiser", "organiser-enrol@test.com", "Password123!", "0711111114", "Organiser"));
        await LoginWithSessionAsync(organiser, new LoginRequest(
            "organiser-enrol@test.com", "Password123!"));

        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new EventCreateRequest(
            "Enrolment Event", "Test enrolment", DateTime.UtcNow.AddDays(7), null, "Polokwane", 10, "Run", null, null));
        eventResponse.EnsureSuccessStatusCode();
        var createdEvent = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(createdEvent);

        var categoryResponse = await organiser.PostAsJsonAsync("/api/categories", new CategoryRequest(
            "10 KM Test", "Distance", "Run", 10, null, null, "Test category"));
        categoryResponse.EnsureSuccessStatusCode();
        var category = await categoryResponse.Content.ReadFromJsonAsync<RaceDay.API.Models.Category>();
        Assert.NotNull(category);

        var attachResponse = await organiser.PostAsJsonAsync(
            $"/api/categories/event/{createdEvent!.EventID}/category/{category!.CategoryID}", 100m);
        attachResponse.EnsureSuccessStatusCode();
        var attached = await attachResponse.Content.ReadFromJsonAsync<RaceDay.API.Models.EventCategory>();
        Assert.NotNull(attached);

        using var participant = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await participant.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "Paula", "Participant", "participant-enrol@test.com", "Password123!", "0711111115", "Participant"));
        await LoginWithSessionAsync(participant, new LoginRequest(
            "participant-enrol@test.com", "Password123!"));

        var enrolmentResponse = await participant.PostAsJsonAsync("/api/enrolments",
            new EnrolmentRequest(attached!.EventCategoryID, "R100"));

        Assert.Equal(HttpStatusCode.Created, enrolmentResponse.StatusCode);
        var enrolment = await enrolmentResponse.Content.ReadFromJsonAsync<RaceDay.API.Models.EventEnrolment>();
        Assert.NotNull(enrolment);
        Assert.Equal("Confirmed", enrolment!.EnrolmentStatus);
    }

    private static async Task<HttpResponseMessage> LoginWithSessionAsync(
        HttpClient client,
        LoginRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        if (response.IsSuccessStatusCode)
        {
            // The API uses ASP.NET Core session authentication, not JWT.
            // Forward the session cookie explicitly to later requests.
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                var sessionCookie = setCookieHeaders
                    .Select(header => header.Split(';', 2)[0])
                    .FirstOrDefault(cookie => cookie.StartsWith(
                        ".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(sessionCookie))
                {
                    client.DefaultRequestHeaders.Remove("Cookie");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", sessionCookie);
                }
            }
        }

        return response;
    }
}
