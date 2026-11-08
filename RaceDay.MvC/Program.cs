using RaceDay.MvC.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.Cookie.Name = ".RaceDay.MvC.Session"; o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; o.IdleTimeout = TimeSpan.FromMinutes(30); });
builder.Services.AddHttpClient("RaceDayApi", client => { var url = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001/"; client.BaseAddress = new Uri(url); client.Timeout = TimeSpan.FromSeconds(30); });
builder.Services.AddScoped<RaceDayApiClient>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting(); app.UseSession(); app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();