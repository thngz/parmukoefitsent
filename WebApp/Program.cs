using App.DAL;
using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.EntityFrameworkCore;
using WebApp;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString ??
                      throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.")).EnableSensitiveDataLogging());

builder.Services.AddDefaultBehaviors();
builder.Services.AddConfiguredHangfire();
builder.Services.AddWorkers();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions()
{
    Authorization = new []
    {
        new HangfireCustomBasicAuthenticationFilter
        {
            User = builder.Configuration.GetValue<string>("HangFire:Username"),
            Pass = builder.Configuration.GetValue<string>("HangFire:Password")
        }
    }
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseWorker();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapControllers();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();