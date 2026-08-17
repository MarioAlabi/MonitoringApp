using System.Net.Http.Json;
using MonitoringApp.Monitoring.Core.DTOs;

namespace MonitoringApp.Monitoring.Agent.Services;

public class AgentWorkerService
{
    private readonly DockerMonitorService _dockerService;
    private readonly SslMonitorService _sslService;
    private readonly HttpClient _httpClient;

    // Configuración del Agente
    private const string ServerUrl = "http://localhost:5179/api/HeartbeatApi";
    private const string Token = "sec-token-truenas-2026-xyz";
    private readonly List<string> _hostsParaAuditar = new() { "google.com:443" }; // Agrega hosts o dominios locales

    public AgentWorkerService()
    {
        _dockerService = new DockerMonitorService();
        _sslService = new SslMonitorService();

        // Handler que ignora errores SSL en entorno de pruebas local
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient = new HttpClient(handler);
    }

    public async Task IniciarBucleAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[Agente] Iniciando servicio de recolección...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine($"[Agente {DateTime.Now:HH:mm:ss}] Recolectando métricas locales...");

                var contenedores = await _dockerService.ObtenerContenedoresAsync();
                var certs = await _sslService.AuditarCertificadosAsync(_hostsParaAuditar);

                var payload = new HeartbeatPayloadDto
                {
                    TokenAutenticacion = Token,
                    IpDireccion = "127.0.0.1",
                    Contenedores = contenedores,
                    Certificados = certs
                };

                var respuesta = await _httpClient.PostAsJsonAsync(ServerUrl, payload, cancellationToken);

                if (respuesta.IsSuccessStatusCode)
                {
                    Console.WriteLine("[Agente] Latido enviado con éxito (200 OK).");
                }
                else
                {
                    Console.WriteLine($"[Agente] Error del servidor: {respuesta.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agente] Falla en la ejecución del ciclo: {ex.Message}");
            }

            // Esperar 60 segundos antes del siguiente latido
            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
        }
    }
}