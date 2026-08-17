using System.Net;
using System.Net.Mail;
using MonitoringApp.Monitoring.Core.Models;
using MonitoringApp.Monitoring.Server.Data;

namespace MonitoringApp.Monitoring.Server.Services;

public class EmailAlertService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    public EmailAlertService(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    public async Task EnviarAlertaAsync(string tipoOrigen, string mensaje)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

        var configuracion = db.ConfiguracionesAlertas.FirstOrDefault() 
            ?? new ConfiguracionAlerta { CorreoDestinatario = "admin@example.com" };

        var smtpSection = _config.GetSection("SmtpSettings");
        var host = smtpSection["Host"] ?? "localhost";
        var port = int.TryParse(smtpSection["Port"], out var p) ? p : 25;
        var usuario = smtpSection["Usuario"];
        var password = smtpSection["Password"];
        var remitente = smtpSection["Remitente"] ?? "no-reply@monitoring.local";

        var logAlerta = new HistorialAlerta
        {
            TipoOrigen = tipoOrigen,
            Mensaje = mensaje,
            DestinatarioCorreo = configuracion.CorreoDestinatario,
            FechaCreacion = DateTime.UtcNow
        };

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = bool.TryParse(smtpSection["EnableSsl"], out var ssl) && ssl,
                Credentials = !string.IsNullOrEmpty(usuario) ? new NetworkCredential(usuario, password) : null
            };

            var mail = new MailMessage(remitente, configuracion.CorreoDestinatario)
            {
                Subject = $"[ALERTA MONITOREO] {tipoOrigen}",
                Body = mensaje,
                IsBodyHtml = false
            };

            await client.SendMailAsync(mail);
            logAlerta.EnviadoExito = true;
            Console.WriteLine($"[SMTP] Alerta enviada con éxito: {tipoOrigen}");
        }
        catch (Exception ex)
        {
            logAlerta.EnviadoExito = false;
            logAlerta.ErrorDetalle = ex.Message;
            Console.WriteLine($"[SMTP] Error al enviar correo: {ex.Message}");
        }

        db.HistorialAlertas.Add(logAlerta);
        await db.SaveChangesAsync();
    }
}