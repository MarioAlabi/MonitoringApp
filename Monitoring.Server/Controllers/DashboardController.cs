using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Core.Models;
using MonitoringApp.Monitoring.Server.Data;

namespace MonitoringApp.Monitoring.Server.Controllers;

[Authorize]
[Route("")]
[Route("[controller]")]
public class DashboardController : Controller
{
    private readonly MonitoringDbContext _context;

    public DashboardController(MonitoringDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.Nodos = await _context.Nodos
            .Include(n => n.Contenedores)
            .Include(n => n.CertificadosSsl)
            .ToListAsync();

        ViewBag.Dominios = await _context.DominiosExpiracion.ToListAsync();
        ViewBag.Alertas = await _context.HistorialAlertas
            .OrderByDescending(a => a.FechaCreacion)
            .Take(10)
            .ToListAsync();

        return View();
    }

    [HttpPost("CrearNodo")]
    public async Task<IActionResult> CrearNodo(string nombre, string? ip)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var nuevoNodo = new Nodo
            {
                Nombre = nombre,
                IpDireccion = ip ?? "0.0.0.0",
                TokenAutenticacion = Guid.NewGuid().ToString("N"),
                Estado = "OFFLINE"
            };
            
            _context.Nodos.Add(nuevoNodo);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("AgregarDominio")]
    public async Task<IActionResult> AgregarDominio(string nombreDominio, string? proveedor)
    {
        if (!string.IsNullOrWhiteSpace(nombreDominio))
        {
            _context.DominiosExpiracion.Add(new DominioExpiracion
            {
                NombreDominio = nombreDominio.Trim(),
                ProveedorRegistro = proveedor ?? "Cloudflare/RDAP",
                UltimaConsulta = DateTime.UtcNow
            });
            
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}