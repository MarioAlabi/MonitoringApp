using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Server.Data;

namespace MonitoringApp.Monitoring.Server.Services;

public class RdapDomainService
{
    private readonly HttpClient _httpClient = new();

    public async Task ActualizarExpiracionDominiosAsync(MonitoringDbContext db)
    {
        var dominios = await db.DominiosExpiracion.ToListAsync();

        foreach (var dom in dominios)
        {
            try
            {
                var url = $"https://rdap.org/domain/{dom.NombreDominio}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    if (doc.RootElement.TryGetProperty("events", out var events))
                    {
                        foreach (var ev in events.EnumerateArray())
                        {
                            if (ev.TryGetProperty("eventAction", out var action) && action.GetString() == "expiration")
                            {
                                if (ev.TryGetProperty("eventDate", out var eventDate) && 
                                    DateTime.TryParse(eventDate.GetString(), out var fechaExp))
                                {
                                    dom.FechaExpiracion = fechaExp.ToUniversalTime();
                                    dom.DiasRestantes = (int)(dom.FechaExpiracion.Value - DateTime.UtcNow).TotalDays;
                                    dom.UltimaConsulta = DateTime.UtcNow;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RDAP] Error consultando dominio {dom.NombreDominio}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync();
    }
}