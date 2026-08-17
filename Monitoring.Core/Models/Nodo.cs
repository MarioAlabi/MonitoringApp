using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringApp.Monitoring.Core.Models;

[Table("nodos")]
public class Nodo
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(45)]
    [Column("ip_direccion")]
    public string? IpDireccion { get; set; }

    [Required]
    [MaxLength(128)]
    [Column("token_autenticacion")]
    public string TokenAutenticacion { get; set; } = string.Empty;

    [Column("ultimo_latido")]
    public DateTime? UltimoLatido { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("estado")]
    public string Estado { get; set; } = "OFFLINE"; // ONLINE, OFFLINE, WARNING

    [Column("alerta_caida_enviada")]
    public bool AlertaCaidaEnviada { get; set; } = false;

    // Relaciones
    public ICollection<Contenedor> Contenedores { get; set; } = new List<Contenedor>();
    public ICollection<CertificadoSsl> CertificadosSsl { get; set; } = new List<CertificadoSsl>();}