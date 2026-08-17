using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringApp.Monitoring.Core.Models;

[Table("dominios_cloudflare")]
public class DominioExpiracion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("nombre_dominio")]
    public string NombreDominio { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("proveedor_registro")]
    public string? ProveedorRegistro { get; set; } // ej. Cloudflare, Namecheap, GoDaddy

    [Column("fecha_expiracion")]
    public DateTime? FechaExpiracion { get; set; }

    [Column("dias_restantes")]
    public int? DiasRestantes { get; set; }

    [Column("ultima_consulta")]
    public DateTime UltimaConsulta { get; set; } = DateTime.UtcNow;

    [Column("alerta_expiracion_enviada")]
    public bool AlertaExpiracionEnviada { get; set; } = false;
}