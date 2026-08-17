using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using MonitoringApp.Monitoring.Core.DTOs;

namespace MonitoringApp.Monitoring.Agent.Services;

public class SslMonitorService
{
    public async Task<List<CertificadoSslDto>> AuditarCertificadosAsync(List<string> hosts)
    {
        var resultados = new List<CertificadoSslDto>();

        foreach (var hostStr in hosts)
        {
            if (string.IsNullOrWhiteSpace(hostStr)) continue;

            var partes = hostStr.Split(':');
            var host = partes[0].Trim();
            var puerto = partes.Length > 1 && int.TryParse(partes[1], out var p) ? p : 443;

            try
            {
                using var tcpClient = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await tcpClient.ConnectAsync(host, puerto, cts.Token);

                using var sslStream = new SslStream(
                    tcpClient.GetStream(),
                    false,
                    // Acepta certificados autofirmados/locales para extraer la metadata sin lanzar excepción
                    (sender, certificate, chain, sslPolicyErrors) => true
                );

                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = host
                });

                if (sslStream.RemoteCertificate != null)
                {
                    using var cert2 = new X509Certificate2(sslStream.RemoteCertificate);
                    var diasRestantes = (int)(cert2.NotAfter - DateTime.UtcNow).TotalDays;

                    resultados.Add(new CertificadoSslDto
                    {
                        DominioHost = host,
                        Puerto = puerto,
                        FechaExpiracion = cert2.NotAfter.ToUniversalTime(),
                        DiasRestantes = diasRestantes
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agente SSL] Error al auditar {host}:{puerto} -> {ex.Message}");
            }
        }

        return resultados;
    }
}