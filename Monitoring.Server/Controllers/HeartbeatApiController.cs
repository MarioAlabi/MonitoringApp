using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Core.DTOs;
using MonitoringApp.Monitoring.Core.Models;
using MonitoringApp.Monitoring.Server.Data;

namespace MonitoringApp.Monitoring.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HeartbeatApiController : ControllerBase
{
    private readonly MonitoringDbContext _context;

    public HeartbeatApiController(MonitoringDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<HeartbeatResponseDto>> RecibirLatido([FromBody] HeartbeatPayloadDto payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.TokenAutenticacion))
        {
            return BadRequest(new HeartbeatResponseDto { Exito = false, Mensaje = "Payload o Token inválido." });
        }

        var nodo = await _context.Nodos
            .Include(n => n.Contenedores)
            .Include(n => n.CertificadosSsl)
            .FirstOrDefaultAsync(n => n.TokenAutenticacion == payload.TokenAutenticacion);

        if (nodo == null)
        {
            return Unauthorized(new HeartbeatResponseDto { Exito = false, Mensaje = "Token no autorizado." });
        }

        // Actualizar datos del nodo
        nodo.UltimoLatido = DateTime.UtcNow;
        nodo.Estado = "ONLINE";
        nodo.AlertaCaidaEnviada = false;
        if (!string.IsNullOrWhiteSpace(payload.IpDireccion))
        {
            nodo.IpDireccion = payload.IpDireccion;
        }

        // Sincronizar contenedores y certificados
        _context.Contenedores.RemoveRange(nodo.Contenedores);
        _context.CertificadosSsl.RemoveRange(nodo.CertificadosSsl);

        if (payload.Contenedores != null && payload.Contenedores.Any())
        {
            foreach (var c in payload.Contenedores)
            {
                nodo.Contenedores.Add(new Contenedor
                {
                    Nombre = c.Nombre,
                    Imagen = c.Imagen,
                    Estado = c.Estado,
                    UltimaActualizacion = DateTime.UtcNow
                });
            }
        }

        if (payload.Certificados != null && payload.Certificados.Any())
        {
            foreach (var cert in payload.Certificados)
            {
                nodo.CertificadosSsl.Add(new CertificadoSsl
                {
                    DominioHost = cert.DominioHost,
                    Puerto = cert.Puerto,
                    FechaExpiracion = cert.FechaExpiracion,
                    DiasRestantes = cert.DiasRestantes,
                    UltimaRevision = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        // Responder al cliente con su configuración
        return Ok(new HeartbeatResponseDto
        {
            Exito = true,
            Mensaje = "Sincronizado correctamente.",
            NuevoIntervaloSegundos = 60,
            NuevosHostsSsl = new List<string> { "google.com:443" }
        });
    }
}