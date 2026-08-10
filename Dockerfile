# ═══════════════════════════════════════════════════════════════════
#  KZTEK ADB Publish Tool — Dockerfile
#  Base  : .NET 8 ASP.NET Core (Debian Bookworm slim)
#  ADB   : android-tools-adb → symlink /opt/platform-tools/adb
#  Data  : /app/data (SQLite DB), /app/uploads (APK files)
#  Port  : 8080  (set via ASPNETCORE_URLS)
# ═══════════════════════════════════════════════════════════════════

# ─── Stage 1: Build ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy NuGet config + csproj trước để cache restore layer
COPY NuGet.Config ./
COPY src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj \
     src/KztekAdbPublishTool.Web/

RUN dotnet restore src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj

# Copy toàn bộ source (bao gồm wwwroot/lib/* — Bootstrap + SignalR local)
COPY src/KztekAdbPublishTool.Web/ src/KztekAdbPublishTool.Web/

RUN dotnet publish src/KztekAdbPublishTool.Web/KztekAdbPublishTool.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─── Stage 2: Runtime ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Cài ADB từ Debian package repo
# adb được symlink về /opt/platform-tools/adb để khớp appsettings.json
RUN apt-get update && \
    apt-get install -y --no-install-recommends android-tools-adb curl && \
    mkdir -p /opt/platform-tools && \
    ln -sf /usr/bin/adb /opt/platform-tools/adb && \
    rm -rf /var/lib/apt/lists/*

# Copy published output từ build stage
COPY --from=build /app/publish .

# Tạo thư mục data/uploads (sẽ được mount qua volume)
RUN mkdir -p /app/data /app/uploads

# Volume để persist dữ liệu qua container restart
VOLUME ["/app/data", "/app/uploads"]

# Kestrel lắng nghe trên tất cả interfaces — cần thiết để truy cập từ LAN
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "KztekAdbPublishTool.Web.dll"]
