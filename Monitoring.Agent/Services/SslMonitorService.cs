using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MonitoringApp.Monitoring.Core.DTOs;

namespace MonitoringApp.Monitoring.Agent.Services;

public class SslMonitorService
{
    public async Task<List<CertificadoSslDto>> AuditarCertificadosAsync(List<string> hosts)
    {
        var certs = new List<CertificadoSslDto>();

        foreach (var host in hosts)
        {
            var partes = host.Split(':');
            var dominio = partes[0].Trim();
            var puerto = partes.Length > 1 && int.TryParse(partes[1], out var p) ? p : 443;

            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(dominio, puerto);

                using var sslStream = new SslStream(
                    tcpClient.GetStream(),
                    false,
                    (sender, certificate, chain, sslPolicyErrors) => true // Acepta certs para poder leer la fecha incluso si hay advertencias
                );

                await sslStream.AuthenticateAsClientAsync(dominio);

                if (sslStream.RemoteCertificate is X509Certificate2 cert)
                {
                    var expDate = cert.NotAfter.ToUniversalTime();
                    var dias = (int)(expDate - DateTime.UtcNow).TotalDays;

                    certs.Add(new CertificadoSslDto
                    {
                        DominioHost = dominio,
                        Puerto = puerto,
                        FechaExpiracion = expDate,
                        DiasRestantes = dias
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agente] Error al auditar SSL en {dominio}:{puerto} -> {ex.Message}");
            }
        }

        return certs;
    }
}