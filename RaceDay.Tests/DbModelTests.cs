using Microsoft.EntityFrameworkCore;
using Xunit;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.Tests;

public class DbModelTests
{
    private static RaceDayDbContext CreateDb()
    {
        var options=new DbContextOptionsBuilder<RaceDayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new RaceDayDbContext(options);
    }

    [Fact]
    public async Task Participant_CanBeStored()
    {
        await using var db=CreateDb();
        db.Users.Add(new User{FirstName="Test",LastName="User",Email="test@test.com",PasswordHash="hash",Role="Participant"});
        await db.SaveChangesAsync();
        Assert.Equal(1,await db.Users.CountAsync());
        Assert.Equal("Participant",(await db.Users.FirstAsync()).Role);
    }

    [Fact]
    public async Task Enrolment_LinksParticipantToEventCategory()
    {
        await using var db=CreateDb();
        var user=new User{FirstName="P",LastName="One",Email="p@x.com",PasswordHash="h",Role="Participant"};
        var org=new User{FirstName="O",LastName="One",Email="o@x.com",PasswordHash="h",Role="Organiser"};
        db.Users.AddRange(user,org);await db.SaveChangesAsync();
        var ev=new Event{OrganiserID=org.UserID,EventName="Test Run",EventDate=DateTime.UtcNow,Location="Test",Distance=10,EventType="Run"};
        var cat=new Category{CategoryName="10 KM",CategoryType="Distance",ActivityType="Run",Distance=10};
        db.Events.Add(ev);db.Categories.Add(cat);await db.SaveChangesAsync();
        var ec=new EventCategory{EventID=ev.EventID,CategoryID=cat.CategoryID};db.EventCategories.Add(ec);await db.SaveChangesAsync();
        db.EventEnrolments.Add(new EventEnrolment{UserID=user.UserID,EventCategoryID=ec.EventCategoryID,EnrolmentStatus="Confirmed"});await db.SaveChangesAsync();
        Assert.Equal(1,await db.EventEnrolments.CountAsync());
    }
}
