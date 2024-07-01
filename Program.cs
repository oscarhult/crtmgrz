using crtmgrz;
using crtmgrz.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dbPath = "./data";

if (!Directory.Exists(dbPath))
{
    Directory.CreateDirectory(dbPath);
}

builder.Services.AddDbContext<CertificatesContext>(options => options.UseSqlite($"Data Source={dbPath}/db.sqlite"));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CertificatesContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
