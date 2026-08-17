namespace MonitoringApp.Monitoring.Core.DTOs;

public class HeartbeatResponseDto
{
    public bool Exito { get; set; } = true;
    public string Mensaje { get; set; } = "OK";
    
    // Configuración remota dinámica
    public int? NuevoIntervaloSegundos { get; set; }
    public List<string>? NuevosHostsSsl { get; set; }

    // Comandos pendientes para que el agente ejecute
    public List<ComandoRemotoDto> ComandosPendientes { get; set; } = new();
}

public class ComandoRemotoDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Accion { get; set; } = string.Empty; // ej: "PING", "RESTART_CONTAINER", "CUSTOM_CLI"
    public string Parametro { get; set; } = string.Empty;
}