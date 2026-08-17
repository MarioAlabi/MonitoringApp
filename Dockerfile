# 1. Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MonitoringApp.csproj", "./"]
RUN dotnet restore "./MonitoringApp.csproj"

COPY . .
RUN dotnet publish "MonitoringApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Carpeta persistente para la base de datos SQLite
VOLUME /app/data
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/monitoring.db"
ENV ASPNETCORE_URLS="http://+:8080"

EXPOSE 8080

ENTRYPOINT ["dotnet", "MonitoringApp.dll"]