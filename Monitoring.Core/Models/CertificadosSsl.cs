using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringApp.Monitoring.Core.Models;

[Table("certificados_ssl")]
public class CertificadoSsl
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nodo_id")]
    public int? NodoId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("dominio_host")]
    public string DominioHost { get; set; } = string.Empty;

    [Column("puerto")]
    public int Puerto { get; set; } = 443;

    [Column("fecha_expiracion")]
    public DateTime? FechaExpiracion { get; set; }

    [Column("dias_restantes")]
    public int? DiasRestantes { get; set; }

    [Column("ultima_revision")]
    public DateTime UltimaRevision { get; set; } = DateTime.UtcNow;

    [Column("alerta_expiracion_enviada")]
    public bool AlertaExpiracionEnviada { get; set; } = false;

    // Relación opcional con el Nodo
    [ForeignKey(nameof(NodoId))]
    public Nodo? Nodo { get; set; }
}