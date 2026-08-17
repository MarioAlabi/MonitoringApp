using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringApp.Monitoring.Core.Models;

[Table("configuracion_alertas")]
public class ConfiguracionAlerta
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("correo_destinatario")]
    public string CorreoDestinatario { get; set; } = string.Empty;

    [Column("timeout_segundos_nodo")]
    public int TimeoutSegundosNodo { get; set; } = 120; // Si no reporta en 2 min -> Caído

    [Column("dias_aviso_ssl")]
    public int DiasAvisoSsl { get; set; } = 15; // Alertar si quedan <= 15 días

    [Column("dias_aviso_dominio")]
    public int DiasAvisoDominio { get; set; } = 30; // Alertar si quedan <= 30 días
}