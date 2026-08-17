using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Agent.Services;
using MonitoringApp.Monitoring.Server.Data;
using MonitoringApp.Monitoring.Server.Services;

// ==========================================
// 1. MODO AGENTE (CLIENTE RECOLECTOR PURO)
// ==========================================
if (Environment.GetEnvironmentVariable("RUN_AS_AGENT") == "true")
{
    Console.WriteLine("[Agente] Iniciando en modo standalone (sin servidor web/BD)...");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    var agent = new AgentWorkerService();
    await agent.IniciarBucleAsync(cts.Token);
    return;
}

// ==========================================
// 2. MODO SERVIDOR (ASP.NET CORE + EF CORE + API)
// ==========================================
var builder = WebApplication.CreateBuilder(args);

// Registro de Servicios y Dependencias
builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Monitoring.Server/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Monitoring.Server/Views/Shared/{0}.cshtml");
    });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddSingleton<EmailAlertService>();
builder.Services.AddHostedService<MonitoringBackgroundService>();

var app = builder.Build();

// Inicialización de la Base de Datos SQLite
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
    db.Database.EnsureCreated();
}

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();