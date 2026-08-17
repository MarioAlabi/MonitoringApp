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
            .Take(15)
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

    [HttpPost("EliminarNodo")]
    public async Task<IActionResult> EliminarNodo(int id)
    {
        var nodo = await _context.Nodos
            .Include(n => n.Contenedores)
            .Include(n => n.CertificadosSsl)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (nodo != null)
        {
            _context.Contenedores.RemoveRange(nodo.Contenedores);
            _context.CertificadosSsl.RemoveRange(nodo.CertificadosSsl);
            _context.Nodos.Remove(nodo);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("RenovarToken")]
    public async Task<IActionResult> RenovarToken(int id, string? nuevoTokenPersonalizado)
    {
        var nodo = await _context.Nodos.FindAsync(id);
        if (nodo != null)
        {
            nodo.TokenAutenticacion = !string.IsNullOrWhiteSpace(nuevoTokenPersonalizado)
                ? nuevoTokenPersonalizado.Trim()
                : Guid.NewGuid().ToString("N");

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

    [HttpPost("EliminarDominio")]
    public async Task<IActionResult> EliminarDominio(int id)
    {
        var dominio = await _context.DominiosExpiracion.FindAsync(id);
        if (dominio != null)
        {
            _context.DominiosExpiracion.Remove(dominio);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("LimpiarAlertas")]
    public async Task<IActionResult> LimpiarAlertas()
    {
        var alertas = await _context.HistorialAlertas.ToListAsync();
        _context.HistorialAlertas.RemoveRange(alertas);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("EliminarAlerta")]
    public async Task<IActionResult> EliminarAlerta(int id)
    {
        var alerta = await _context.HistorialAlertas.FindAsync(id);
        if (alerta != null)
        {
            _context.HistorialAlertas.Remove(alerta);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}