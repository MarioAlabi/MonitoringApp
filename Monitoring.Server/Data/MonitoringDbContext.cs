using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Core.Models;

namespace MonitoringApp.Monitoring.Server.Data;

public class MonitoringDbContext : DbContext
{
    public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options) : base(options)
    {
    }

    public DbSet<Nodo> Nodos => Set<Nodo>();
    public DbSet<Contenedor> Contenedores => Set<Contenedor>();
    public DbSet<CertificadoSsl> CertificadosSsl => Set<CertificadoSsl>();
    public DbSet<DominioExpiracion> DominiosExpiracion => Set<DominioExpiracion>();
    public DbSet<HistorialAlerta> HistorialAlertas => Set<HistorialAlerta>();
    public DbSet<ConfiguracionAlerta> ConfiguracionesAlertas => Set<ConfiguracionAlerta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Índices para búsquedas rápidas
        modelBuilder.Entity<Nodo>()
            .HasIndex(n => n.TokenAutenticacion)
            .IsUnique();

        modelBuilder.Entity<Contenedor>()
            .HasIndex(c => new { c.NodoId, c.Nombre });

        modelBuilder.Entity<CertificadoSsl>()
            .HasIndex(c => c.DominioHost);

        modelBuilder.Entity<DominioExpiracion>()
            .HasIndex(d => d.NombreDominio)
            .IsUnique();

        // Seed inicial por defecto
        modelBuilder.Entity<ConfiguracionAlerta>().HasData(
            new ConfiguracionAlerta
            {
                Id = 1,
                CorreoDestinatario = "admin@example.com",
                TimeoutSegundosNodo = 120,
                DiasAvisoSsl = 15,
                DiasAvisoDominio = 30
            }
        );

        modelBuilder.Entity<Nodo>().HasData(
            new Nodo
            {
                Id = 1,
                Nombre = "TrueNAS Local",
                IpDireccion = "127.0.0.1",
                TokenAutenticacion = "sec-token-truenas-2026-xyz",
                Estado = "OFFLINE",
                UltimoLatido = null
            }
        );
    }
}