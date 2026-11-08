using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using RaceDay.API.Data;
using RaceDay.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure listening URLs. If ASPNETCORE_URLS is set use that, otherwise fall back to ephemeral loopback ports
// This avoids "address already in use" failures when running multiple instances locally.
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://127.0.0.1:0;https://127.0.0.1:0";
builder.WebHost.UseUrls(urls);

builder.Services.AddDbContext<RaceDayDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayConnection")));
builder.Services.AddScoped<PasswordService>();
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => { options.IdleTimeout = TimeSpan.FromMinutes(30); options.Cookie.HttpOnly = true; options.Cookie.IsEssential = true; options.Cookie.SameSite = SameSiteMode.Lax; });
// Ensure tests using TestServer (which bind to port 0) don't cause Https redirection to throw.
builder.Services.AddHttpsRedirection(options => { options.HttpsPort = 443; });
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        IF COL_LENGTH('dbo.Users', 'PhoneNumber') IS NULL
            ALTER TABLE [dbo].[Users] ADD [PhoneNumber] nvarchar(255) NULL;
        IF COL_LENGTH('dbo.Users', 'CreatedAt') IS NULL
            ALTER TABLE [dbo].[Users] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME();
        IF COL_LENGTH('dbo.Users', 'ProfileImageBlobName') IS NULL
            ALTER TABLE [dbo].[Users] ADD [ProfileImageBlobName] nvarchar(500) NULL;
        IF COL_LENGTH('dbo.Users', 'ProfileImageContentType') IS NULL
            ALTER TABLE [dbo].[Users] ADD [ProfileImageContentType] nvarchar(100) NULL;
        IF COL_LENGTH('dbo.Events', 'EventName') IS NULL
            ALTER TABLE [dbo].[Events] ADD [EventName] nvarchar(200) NOT NULL CONSTRAINT [DF_Events_EventName] DEFAULT N'Unnamed event';
        IF COL_LENGTH('dbo.Events', 'StartTime') IS NULL
            ALTER TABLE [dbo].[Events] ADD [StartTime] time NULL;
        IF COL_LENGTH('dbo.Events', 'StartLocation') IS NULL
            ALTER TABLE [dbo].[Events] ADD [StartLocation] nvarchar(255) NULL;
        IF COL_LENGTH('dbo.Events', 'FinishLocation') IS NULL
            ALTER TABLE [dbo].[Events] ADD [FinishLocation] nvarchar(255) NULL;
        IF COL_LENGTH('dbo.Events', 'Status') IS NULL
            ALTER TABLE [dbo].[Events] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_Events_Status] DEFAULT N'Open';
        IF COL_LENGTH('dbo.Events', 'CreatedAt') IS NULL
            ALTER TABLE [dbo].[Events] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Events_CreatedAt] DEFAULT SYSUTCDATETIME();
        IF COL_LENGTH('dbo.Events', 'BannerBlobName') IS NULL
            ALTER TABLE [dbo].[Events] ADD [BannerBlobName] nvarchar(500) NULL;
        IF COL_LENGTH('dbo.Events', 'BannerContentType') IS NULL
            ALTER TABLE [dbo].[Events] ADD [BannerContentType] nvarchar(100) NULL;
        IF COL_LENGTH('dbo.Events', 'Name') IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('dbo.Events') AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Events'), 'Name', 'ColumnId'))
            ALTER TABLE [dbo].[Events] ADD CONSTRAINT [DF_Events_Name] DEFAULT N'Unnamed event' FOR [Name];
        IF COL_LENGTH('dbo.Categories', 'CategoryType') IS NULL
            ALTER TABLE [dbo].[Categories] ADD [CategoryType] nvarchar(50) NOT NULL CONSTRAINT [DF_Categories_CategoryType] DEFAULT N'Age';
        IF COL_LENGTH('dbo.Categories', 'ActivityType') IS NULL
            ALTER TABLE [dbo].[Categories] ADD [ActivityType] nvarchar(50) NOT NULL CONSTRAINT [DF_Categories_ActivityType] DEFAULT N'Run';
        IF COL_LENGTH('dbo.Categories', 'Distance') IS NULL
            ALTER TABLE [dbo].[Categories] ADD [Distance] decimal(8,2) NULL;
        IF COL_LENGTH('dbo.Categories', 'MinimumAge') IS NULL
            ALTER TABLE [dbo].[Categories] ADD [MinimumAge] int NULL;
        IF COL_LENGTH('dbo.Categories', 'MaximumAge') IS NULL
            ALTER TABLE [dbo].[Categories] ADD [MaximumAge] int NULL;
        IF COL_LENGTH('dbo.Categories', 'Description') IS NULL
            ALTER TABLE [dbo].[Categories] ADD [Description] nvarchar(max) NULL;
        IF OBJECT_ID('dbo.EventEnrolments', 'U') IS NULL
            CREATE TABLE [dbo].[EventEnrolments]
            (
                [EnrolmentID] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EventEnrolments] PRIMARY KEY,
                [UserID] int NOT NULL,
                [EventCategoryID] int NOT NULL,
                [EnrolmentDate] datetime2 NOT NULL CONSTRAINT [DF_EventEnrolments_EnrolmentDate] DEFAULT SYSUTCDATETIME(),
                [RaceNumber] nvarchar(50) NULL,
                [EnrolmentStatus] nvarchar(20) NOT NULL CONSTRAINT [DF_EventEnrolments_EnrolmentStatus] DEFAULT N'Pending'
            );
        IF OBJECT_ID('dbo.EventCategories', 'U') IS NULL
            CREATE TABLE [dbo].[EventCategories]
            (
                [EventCategoryID] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EventCategories] PRIMARY KEY,
                [EventID] int NOT NULL,
                [CategoryID] int NOT NULL,
                [EntryFee] decimal(10,2) NOT NULL CONSTRAINT [DF_EventCategories_EntryFee] DEFAULT 0,
                [MaximumParticipants] int NULL
            );
        IF OBJECT_ID('dbo.Results', 'U') IS NULL
            CREATE TABLE [dbo].[Results]
            (
                [ResultID] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Results] PRIMARY KEY,
                [EnrolmentID] int NOT NULL,
                [FinishTime] time NULL,
                [Position] int NULL,
                [CategoryPosition] int NULL,
                [Pace] decimal(8,2) NULL,
                [ResultStatus] nvarchar(20) NOT NULL CONSTRAINT [DF_Results_ResultStatus] DEFAULT N'Finished'
            );
        IF COL_LENGTH('dbo.Results', 'Position') IS NULL
            ALTER TABLE [dbo].[Results] ADD [Position] int NULL;
        IF COL_LENGTH('dbo.Results', 'CategoryPosition') IS NULL
            ALTER TABLE [dbo].[Results] ADD [CategoryPosition] int NULL;
        IF COL_LENGTH('dbo.Results', 'Pace') IS NULL
            ALTER TABLE [dbo].[Results] ADD [Pace] decimal(8,2) NULL;
        IF COL_LENGTH('dbo.Results', 'ResultStatus') IS NULL
            ALTER TABLE [dbo].[Results] ADD [ResultStatus] nvarchar(20) NOT NULL CONSTRAINT [DF_Results_ResultStatus_Existing] DEFAULT N'Finished';
        IF OBJECT_ID('dbo.Routes', 'U') IS NULL
            CREATE TABLE [dbo].[Routes]
            (
                [RouteID] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Routes] PRIMARY KEY,
                [EventID] int NOT NULL,
                [RouteName] nvarchar(200) NOT NULL,
                [Distance] decimal(8,2) NOT NULL,
                [ElevationGain] decimal(10,2) NULL,
                [DifficultyLevel] nvarchar(50) NULL,
                [StartPoint] nvarchar(255) NULL,
                [EndPoint] nvarchar(255) NULL,
                [MapURL] nvarchar(500) NULL,
                [Description] nvarchar(max) NULL
            );
        IF OBJECT_ID('dbo.Weather', 'U') IS NULL
            CREATE TABLE [dbo].[Weather]
            (
                [WeatherID] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Weather] PRIMARY KEY,
                [EventID] int NOT NULL,
                [RecordedAt] datetime2 NOT NULL CONSTRAINT [DF_Weather_RecordedAt] DEFAULT SYSUTCDATETIME(),
                [Temperature] decimal(5,2) NULL,
                [WeatherCondition] nvarchar(100) NULL,
                [Humidity] decimal(5,2) NULL,
                [WindSpeed] decimal(6,2) NULL,
                [RainProbability] decimal(5,2) NULL
            );
        """);
}
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
// Log incoming requests to help diagnose test failures
app.Use(async (context, next) =>
{
    app.Logger.LogInformation("Incoming request {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
});
app.UseSession();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
