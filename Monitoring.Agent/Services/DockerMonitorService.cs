using Docker.DotNet;
using Docker.DotNet.Models;
using MonitoringApp.Monitoring.Core.DTOs;
using System.Runtime.InteropServices;

namespace MonitoringApp.Monitoring.Agent.Services;

public class DockerMonitorService
{
    private readonly DockerClient _client;

    public DockerMonitorService()
    {
        Uri dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");

        _client = new DockerClientConfiguration(dockerUri).CreateClient();
    }

    public async Task<List<ContenedorDto>> ObtenerContenedoresAsync()
    {
        var resultado = new List<ContenedorDto>();

        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true // Incluye contenedores detenidos y en ejecución
            });

            foreach (var c in containers)
            {
                var nombre = c.Names.FirstOrDefault()?.TrimStart('/') ?? "desconocido";
                resultado.Add(new ContenedorDto
                {
                    Nombre = nombre,
                    Imagen = c.Image,
                    Estado = c.State // "running", "exited", "paused", etc.
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Agente] Error al conectar con Docker: {ex.Message}");
        }

        return resultado;
    }
}