using System.Net.Sockets;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MonitoringApp.Monitoring.Server.Data;

namespace MonitoringApp.Monitoring.Server.Services;

public class RdapDomainService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        DefaultRequestHeaders = { { "User-Agent", "InfraMonitor/1.0 (Linux; x64)" } }
    };

    public async Task ActualizarExpiracionDominiosAsync(MonitoringDbContext db)
    {
        var dominios = await db.DominiosExpiracion.ToListAsync();

        foreach (var dom in dominios)
        {
            var dominioLimpio = dom.NombreDominio.Trim().ToLower()
                .Replace("https://", "")
                .Replace("http://", "")
                .Split('/')[0];

            try
            {
                // 1. Intento por RDAP con User-Agent
                var url = $"https://rdap.org/domain/{dominioLimpio}";
                var response = await _httpClient.GetAsync(url);

                bool resuelto = false;

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
                                    resuelto = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // 2. Fallback TCP WHOIS (especialmente útil para .com / .net de Verisign)
                if (!resuelto && dominioLimpio.EndsWith(".com"))
                {
                    var fechaWhois = await ConsultarWhoisVerisignAsync(dominioLimpio);
                    if (fechaWhois.HasValue)
                    {
                        dom.FechaExpiracion = fechaWhois.Value.ToUniversalTime();
                        dom.DiasRestantes = (int)(dom.FechaExpiracion.Value - DateTime.UtcNow).TotalDays;
                        dom.UltimaConsulta = DateTime.UtcNow;
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

    private async Task<DateTime?> ConsultarWhoisVerisignAsync(string dominio)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await client.ConnectAsync("whois.verisign-grs.com", 43, cts.Token);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            using var reader = new StreamReader(stream);

            await writer.WriteLineAsync(dominio);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.Contains("Registry Expiry Date:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Expiration Date:", StringComparison.OrdinalIgnoreCase))
                {
                    var partes = line.Split(':', 2);
                    if (partes.Length > 1 && DateTime.TryParse(partes[1].Trim(), out var fechaExp))
                    {
                        return fechaExp;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WHOIS TCP] Fallback falló para {dominio}: {ex.Message}");
        }

        return null;
    }
}