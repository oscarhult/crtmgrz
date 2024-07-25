using crtmgrz;
using crtmgrz.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var dbPath = isDocker && builder.Environment.IsProduction() ? "/app/data/db.sqlite" : "db.sqlite";

builder.Services.AddDbContextFactory<CertificatesContext>(options => options.UseSqlite($"Data Source={dbPath};"));
builder.Services.AddSingleton<CertificatesService>();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(15);
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CertificatesContext>();
    db.Database.Migrate();
}

app.UseStatusCodePagesWithRedirects("/Error");
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
