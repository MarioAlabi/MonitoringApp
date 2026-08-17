using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Core.Models;
using MonitoringApp.Monitoring.Server.Data;
using Resend;

namespace MonitoringApp.Monitoring.Server.Services;

public class EmailAlertService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IResend _resend;

    public EmailAlertService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") 
                     ?? "re_A4s35Fmj_JbZMf2fTx1We6W4tfhTsPVSD";

        _resend = ResendClient.Create(apiKey);
    }

    public async Task EnviarAlertaAsync(string tipo, string mensaje)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

        var config = await db.ConfiguracionesAlertas.FirstOrDefaultAsync();
        var destinatario = !string.IsNullOrWhiteSpace(config?.CorreoDestinatario) 
            ? config.CorreoDestinatario 
            : "luis.balcaceres@catolica.edu.sv";

        var asunto = tipo switch
        {
            "NODO_CAIDO" => "🔴 [ALERTA] Servidor / Nodo Caído",
            "SSL_EXPIRACION" => "🟡 [AVISO] Certificado SSL por Expirar",
            "DOMINIO_EXPIRACION" => "🟠 [AVISO] Dominio por Expirar",
            _ => "⚡ [ALERTA] Notificación de Infraestructura"
        };

        var html = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                <h2 style='color: #dc2626; margin-top: 0;'>⚡ InfraMonitor - {tipo}</h2>
                <p style='font-size: 15px; color: #334155; line-height: 1.5;'>{mensaje}</p>
                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 20px 0;'/>
                <small style='color: #94a3b8;'>Fecha UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</small>
            </div>";

        bool exito = false;
        try
        {
            var email = new EmailMessage
            {
                From = "onboarding@resend.dev",
                To = destinatario,
                Subject = asunto,
                HtmlBody = html
            };

            var resp = await _resend.EmailSendAsync(email);
            exito = resp.Success;
            Console.WriteLine($"[Resend] Correo enviado a {destinatario} -> Estado: {(exito ? "OK" : "Fallo")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Resend] Error enviando correo: {ex.Message}");
        }

        // Registrar en el historial de alertas
        db.HistorialAlertas.Add(new HistorialAlerta
        {
            TipoOrigen = tipo,
            DestinatarioCorreo = destinatario,
            Mensaje = mensaje,
            EnviadoExito = exito,
            FechaCreacion = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}