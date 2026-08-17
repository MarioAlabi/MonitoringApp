using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Server.Data;

namespace MonitoringApp.Monitoring.Server.Services;

public class MonitoringBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailAlertService _emailService;
    private readonly RdapDomainService _rdapService = new();

    public MonitoringBackgroundService(IServiceScopeFactory scopeFactory, EmailAlertService emailService)
    {
        _scopeFactory = scopeFactory;
        _emailService = emailService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

                var config = await db.ConfiguracionesAlertas.FirstOrDefaultAsync(stoppingToken)
                    ?? new Core.Models.ConfiguracionAlerta();

                // 1. Detección de Nodos Caídos por Timeout de Heartbeat
                var limiteLatido = DateTime.UtcNow.AddSeconds(-config.TimeoutSegundosNodo);
                var nodos = await db.Nodos.ToListAsync(stoppingToken);

                foreach (var nodo in nodos)
                {
                    if (nodo.UltimoLatido == null || nodo.UltimoLatido < limiteLatido)
                    {
                        if (nodo.Estado != "OFFLINE")
                        {
                            nodo.Estado = "OFFLINE";
                            if (!nodo.AlertaCaidaEnviada)
                            {
                                await _emailService.EnviarAlertaAsync(
                                    "NODO_CAIDO",
                                    $"El nodo '{nodo.Nombre}' ({nodo.IpDireccion}) no envía latidos desde {nodo.UltimoLatido} y ha sido marcado como OFFLINE."
                                );
                                nodo.AlertaCaidaEnviada = true;
                            }
                        }
                    }
                }

                // 2. Auditoría de Certificados SSL por vencer
                var certs = await db.CertificadosSsl.Include(c => c.Nodo).ToListAsync(stoppingToken);
                foreach (var cert in certs)
                {
                    if (cert.DiasRestantes.HasValue && cert.DiasRestantes <= config.DiasAvisoSsl && !cert.AlertaExpiracionEnviada)
                    {
                        await _emailService.EnviarAlertaAsync(
                            "SSL_EXPIRACION",
                            $"El certificado SSL para '{cert.DominioHost}:{cert.Puerto}' en el nodo '{cert.Nodo?.Nombre ?? "General"}' vence en {cert.DiasRestantes} días (Expira: {cert.FechaExpiracion})."
                        );
                        cert.AlertaExpiracionEnviada = true;
                    }
                }

                // 3. Revisión y Auditoría de Dominios RDAP / WHOIS
                await _rdapService.ActualizarExpiracionDominiosAsync(db);

                var dominios = await db.DominiosExpiracion.ToListAsync(stoppingToken);
                foreach (var dom in dominios)
                {
                    if (dom.DiasRestantes.HasValue && dom.DiasRestantes <= config.DiasAvisoDominio && !dom.AlertaExpiracionEnviada)
                    {
                        await _emailService.EnviarAlertaAsync(
                            "DOMINIO_EXPIRACION",
                            $"El dominio '{dom.NombreDominio}' ({dom.ProveedorRegistro}) está próximo a expirar. Quedan {dom.DiasRestantes} días (Expira: {dom.FechaExpiracion})."
                        );
                        dom.AlertaExpiracionEnviada = true;
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supervisor] Error en ciclo de monitoreo: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}