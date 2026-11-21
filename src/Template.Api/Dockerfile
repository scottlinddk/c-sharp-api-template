# syntax=docker/dockerfile:1
# Build from the repository root:
#   docker build -f src/Template.Api/Dockerfile -t template-api .                      # API image (default)
#   docker build -f src/Template.Api/Dockerfile --target migrator -t template-migrator . # EF migration bundle

ARG DOTNET_VERSION=11.0

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Restore first, using only project files, so this layer is cached until dependencies change.
COPY global.json Directory.Build.props Directory.Packages.props dotnet-tools.json ./
COPY src/Template.Api/Template.Api.csproj src/Template.Api/
COPY src/Template.Application/Template.Application.csproj src/Template.Application/
COPY src/Template.Domain/Template.Domain.csproj src/Template.Domain/
COPY src/Template.Infrastructure/Template.Infrastructure.csproj src/Template.Infrastructure/
RUN dotnet restore src/Template.Api/Template.Api.csproj

COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Template.Api/Template.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ---------- migration bundle ----------
FROM build AS migrations-build
RUN dotnet tool restore \
 && dotnet ef migrations bundle \
      --project src/Template.Infrastructure \
      --startup-project src/Template.Api \
      --configuration Release \
      --output /app/efbundle

# One-off container: reads ConnectionStrings__Default, applies pending migrations, exits.
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled AS migrator
WORKDIR /app
COPY --from=migrations-build /app/efbundle ./efbundle
COPY --from=build /app/publish/appsettings.json ./
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# ---------- runtime (default target, keep last) ----------
# Chiseled: no shell, no package manager, non-root by default. Health is checked from outside.
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled AS api
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Template.Api.dll"]
