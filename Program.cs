using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Server.Data;
using MonitoringApp.Monitoring.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Registro de Servicios y Dependencias (ANTES de builder.Build)
builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // Limpia las rutas por defecto y agrega las rutas dentro de Monitoring.Server
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

// 2. Construir la aplicación
var app = builder.Build();

// 3. Inicialización de la Base de Datos SQLite
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
    db.Database.EnsureCreated();
}

// 4. Configuración del Pipeline HTTP
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

// 5. Iniciar el Agente en segundo plano
_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    var agent = new MonitoringApp.Monitoring.Agent.Services.AgentWorkerService();
    await agent.IniciarBucleAsync(CancellationToken.None);
});

app.Run();