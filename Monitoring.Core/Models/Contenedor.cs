using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringApp.Monitoring.Core.Models;

[Table("contenedores")]
public class Contenedor
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("nodo_id")]
    public int NodoId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("imagen")]
    public string Imagen { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("estado")]
    public string Estado { get; set; } = "unknown"; // running, exited, paused, etc.

    [Column("ultima_actualizacion")]
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;

    // Relación con Nodo
    [ForeignKey(nameof(NodoId))]
    public Nodo Nodo { get; set; } = null!;
}