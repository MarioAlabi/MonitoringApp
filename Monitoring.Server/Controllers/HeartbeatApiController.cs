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
    public async Task<IActionResult> RecibirLatido([FromBody] HeartbeatPayloadDto payload)
    {
        if (string.IsNullOrWhiteSpace(payload.TokenAutenticacion))
        {
            return Unauthorized(new { error = "Token no proporcionado." });
        }

        // 1. Validar Token contra la base de datos
        var nodo = await _context.Nodos
            .Include(n => n.Contenedores)
            .Include(n => n.CertificadosSsl)
            .FirstOrDefaultAsync(n => n.TokenAutenticacion == payload.TokenAutenticacion);

        if (nodo == null)
        {
            return Unauthorized(new { error = "Token de autenticación inválido." });
        }

        // 2. Actualizar estado y latido del Nodo
        nodo.UltimoLatido = DateTime.UtcNow;
        nodo.Estado = "ONLINE";
        nodo.AlertaCaidaEnviada = false;
        if (!string.IsNullOrEmpty(payload.IpDireccion))
        {
            nodo.IpDireccion = payload.IpDireccion;
        }

        // 3. Sincronizar Contenedores
        _context.Contenedores.RemoveRange(nodo.Contenedores);
        foreach (var c in payload.Contenedores)
        {
            _context.Contenedores.Add(new Contenedor
            {
                NodoId = nodo.Id,
                Nombre = c.Nombre,
                Imagen = c.Imagen,
                Estado = c.Estado,
                UltimaActualizacion = DateTime.UtcNow
            });
        }

        // 4. Sincronizar Certificados SSL
        _context.CertificadosSsl.RemoveRange(nodo.CertificadosSsl);
        foreach (var cert in payload.Certificados)
        {
            _context.CertificadosSsl.Add(new CertificadoSsl
            {
                NodoId = nodo.Id,
                DominioHost = cert.DominioHost,
                Puerto = cert.Puerto,
                FechaExpiracion = cert.FechaExpiracion,
                DiasRestantes = cert.DiasRestantes,
                UltimaRevision = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { status = "success", mensaje = "Heartbeat procesado correctamente." });
    }
}