# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MedLinkPortal.csproj .
RUN dotnet restore MedLinkPortal.csproj

COPY . .
RUN dotnet publish MedLinkPortal.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
# Do NOT set ASPNETCORE_URLS here — Railway injects PORT and Program.cs handles it
ENV ASPNETCORE_HTTP_PORTS=""

EXPOSE 8080

ENTRYPOINT ["dotnet", "MedLinkPortal.dll"]
