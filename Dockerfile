# syntax=docker/dockerfile:1

# --- Build -------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Gestión central de paquetes y props comunes.
COPY Directory.Build.props Directory.Packages.props ./

# Restauración en capa propia para aprovechar la caché: primero solo los csproj.
COPY src/Barrapp.Api/Barrapp.Api.csproj src/Barrapp.Api/
COPY src/Barrapp.Application/Barrapp.Application.csproj src/Barrapp.Application/
COPY src/Barrapp.Domain/Barrapp.Domain.csproj src/Barrapp.Domain/
COPY src/Barrapp.Infrastructure/Barrapp.Infrastructure.csproj src/Barrapp.Infrastructure/
COPY src/Barrapp.Persistence/Barrapp.Persistence.csproj src/Barrapp.Persistence/
RUN dotnet restore src/Barrapp.Api/Barrapp.Api.csproj

COPY src/ src/
RUN dotnet publish src/Barrapp.Api/Barrapp.Api.csproj -c Release -o /app --no-restore

# --- Runtime -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Carpeta de la base SQLite; el volumen de Compose se monta aquí.
USER root
RUN mkdir -p /data && chown -R app:app /data
USER app

VOLUME /data
ENTRYPOINT ["dotnet", "Barrapp.Api.dll"]
