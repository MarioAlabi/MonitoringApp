using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringApp.Monitoring.Core.Models;

[Table("historial_alertas")]
public class HistorialAlerta
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("tipo_origen")]
    public string TipoOrigen { get; set; } = string.Empty; // NODO_CAIDO, CONTENEDOR_STOP, SSL_EXPIRACION, DOMINIO_EXPIRACION

    [Required]
    [Column("mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("destinatario_correo")]
    public string DestinatarioCorreo { get; set; } = string.Empty;

    [Column("enviado_exito")]
    public bool EnviadoExito { get; set; } = true;

    [Column("error_detalle")]
    public string? ErrorDetalle { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}