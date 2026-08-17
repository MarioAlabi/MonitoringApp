namespace MonitoringApp.Monitoring.Core.DTOs;

public class HeartbeatPayloadDto
{
    public string TokenAutenticacion { get; set; } = string.Empty;
    public string? IpDireccion { get; set; }
    public List<ContenedorDto> Contenedores { get; set; } = new();
    public List<CertificadoSslDto> Certificados { get; set; } = new();
}

public class ContenedorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Imagen { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty; // "running", "exited", etc.
}

public class CertificadoSslDto
{
    public string DominioHost { get; set; } = string.Empty;
    public int Puerto { get; set; } = 443;
    public DateTime? FechaExpiracion { get; set; }
    public int? DiasRestantes { get; set; }
}