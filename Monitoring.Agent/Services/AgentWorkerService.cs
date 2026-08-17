using System.Diagnostics;
using System.Net.Http.Json;
using MonitoringApp.Monitoring.Core.DTOs;

namespace MonitoringApp.Monitoring.Agent.Services;

public class AgentWorkerService
{
    private readonly DockerMonitorService _dockerService;
    private readonly SslMonitorService _sslService;
    private readonly HttpClient _httpClient;

    private string _serverUrl;
    private string _token;
    private int _intervaloSegundos = 60;
    private List<string> _hostsParaAuditar;

    public AgentWorkerService()
    {
        _serverUrl = Environment.GetEnvironmentVariable("AGENT_SERVER_URL") 
            ?? "http://92.113.148.5:8585/api/HeartbeatApi";

        _token = Environment.GetEnvironmentVariable("AGENT_TOKEN") 
            ?? "sec-token-truenas-2026-xyz";

        // Lee los hosts desde la variable de entorno AGENT_SSL_HOSTS (separados por coma)
        var envSslHosts = Environment.GetEnvironmentVariable("AGENT_SSL_HOSTS");
        if (!string.IsNullOrWhiteSpace(envSslHosts))
        {
            _hostsParaAuditar = envSslHosts
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
        else
        {
            _hostsParaAuditar = new List<string> { "127.0.0.1:443" };
        }

        _dockerService = new DockerMonitorService();
        _sslService = new SslMonitorService();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient = new HttpClient(handler);
    }

    public async Task IniciarBucleAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Agente] Conectando hacia: {_serverUrl}");
        Console.WriteLine($"[Agente] Hosts SSL a auditar: {string.Join(", ", _hostsParaAuditar)}");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var contenedores = await _dockerService.ObtenerContenedoresAsync();
                var certs = await _sslService.AuditarCertificadosAsync(_hostsParaAuditar);

                var payload = new HeartbeatPayloadDto
                {
                    TokenAutenticacion = _token,
                    IpDireccion = Environment.GetEnvironmentVariable("AGENT_NODE_IP") ?? "127.0.0.1",
                    Contenedores = contenedores,
                    Certificados = certs
                };

                var respuesta = await _httpClient.PostAsJsonAsync(_serverUrl, payload, cancellationToken);

                if (respuesta.IsSuccessStatusCode)
                {
                    var configRespuesta = await respuesta.Content.ReadFromJsonAsync<HeartbeatResponseDto>(cancellationToken: cancellationToken);
                    
                    if (configRespuesta != null)
                    {
                        // 1. Aplicar configuración remota si el servidor la cambia
                        if (configRespuesta.NuevoIntervaloSegundos.HasValue)
                            _intervaloSegundos = configRespuesta.NuevoIntervaloSegundos.Value;

                        if (configRespuesta.NuevosHostsSsl != null && configRespuesta.NuevosHostsSsl.Any())
                            _hostsParaAuditar = configRespuesta.NuevosHostsSsl;

                        // 2. Procesar y ejecutar comandos que mandó el servidor
                        foreach (var cmd in configRespuesta.ComandosPendientes)
                        {
                            EjecutarComando(cmd);
                        }
                    }

                    Console.WriteLine($"[Agente] Latido OK -> {_serverUrl} (Próximo ciclo en {_intervaloSegundos}s)");
                }
                else
                {
                    Console.WriteLine($"[Agente] Error del servidor: {respuesta.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agente] Error de comunicación: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(_intervaloSegundos), cancellationToken);
        }
    }

    private void EjecutarComando(ComandoRemotoDto comando)
    {
        Console.WriteLine($"[Agente] Ejecutando comando remoto: {comando.Accion} ({comando.Parametro})");
        try
        {
            switch (comando.Accion.ToUpper())
            {
                case "PING":
                    Console.WriteLine("[Agente] PONG: El agente está activo y respondiendo.");
                    break;
                case "RESTART_CONTAINER":
                    Process.Start("podman", $"restart {comando.Parametro}");
                    break;
                case "CUSTOM_CLI":
                    Process.Start("/bin/sh", $"-c \"{comando.Parametro}\"");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Agente] Error al ejecutar comando {comando.Accion}: {ex.Message}");
        }
    }
}