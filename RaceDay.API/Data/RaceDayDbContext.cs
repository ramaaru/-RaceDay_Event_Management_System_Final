using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;
using RaceDayRoute = RaceDay.API.Models.Route;
using Route = RaceDay.API.Models.Route;

namespace RaceDay.API.Data;

public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<EventEnrolment> EventEnrolments => Set<EventEnrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<RaceDayRoute> Routes => Set<RaceDayRoute>();
    public DbSet<Weather> Weather => Set<Weather>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().Property(x => x.Email).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<User>().Property(x => x.Role).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<Event>().Property(x => x.Distance).HasPrecision(8,2);
        modelBuilder.Entity<Event>().Property(x => x.EventType).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<EventCategory>().Property(x => x.EntryFee).HasPrecision(10,2);
        modelBuilder.Entity<Category>().Property(x => x.Distance).HasPrecision(8,2);
        modelBuilder.Entity<Result>().Property(x => x.Pace).HasPrecision(8,2);
        modelBuilder.Entity<RaceDayRoute>()
        .Property(x => x.Distance)
        .HasPrecision(8, 2);

        modelBuilder.Entity<RaceDayRoute>()
            .Property(x => x.ElevationGain)
            .HasPrecision(10, 2);
        modelBuilder.Entity<Weather>().Property(x => x.Temperature).HasPrecision(5,2);
        modelBuilder.Entity<Weather>().Property(x => x.Humidity).HasPrecision(5,2);
        modelBuilder.Entity<Weather>().Property(x => x.WindSpeed).HasPrecision(6,2);
        modelBuilder.Entity<Weather>().Property(x => x.RainProbability).HasPrecision(5,2);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany(u => u.Events)
            .HasForeignKey(e => e.OrganiserID).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventCategory>()
            .HasOne(x => x.Event).WithMany(x => x.EventCategories)
            .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventCategory>()
            .HasOne(x => x.Category).WithMany(x => x.EventCategories)
            .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EventCategory>().HasIndex(x => new { x.EventID, x.CategoryID }).IsUnique();

        modelBuilder.Entity<EventEnrolment>()
            .HasKey(x => x.EnrolmentID);
        modelBuilder.Entity<EventEnrolment>()
            .HasOne(x => x.User).WithMany(x => x.EventEnrolments)
            .HasForeignKey(x => x.UserID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EventEnrolment>()
            .HasOne(x => x.EventCategory).WithMany(x => x.Enrolments)
            .HasForeignKey(x => x.EventCategoryID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EventEnrolment>().HasIndex(x => new { x.UserID, x.EventCategoryID }).IsUnique();

        modelBuilder.Entity<Result>()
            .HasOne(x => x.Enrolment).WithOne(x => x.Result)
            .HasForeignKey<Result>(x => x.EnrolmentID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RaceDayRoute>()
            .HasOne(x => x.Event).WithOne(x => x.Route)
            .HasForeignKey<RaceDayRoute>(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Weather>()
            .HasOne(x => x.Event).WithMany(x => x.WeatherRecords)
            .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
    }
}
