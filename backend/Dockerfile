# Contexto de build: backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo lo necesario para restaurar, para aprovechar la caché de capas.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Wallet.Issuer/Wallet.Issuer.csproj src/Wallet.Issuer/
COPY src/Wallet.Tenant/Wallet.Tenant.csproj src/Wallet.Tenant/
COPY src/Wallet.Infrastructure/Wallet.Infrastructure.csproj src/Wallet.Infrastructure/
COPY src/Wallet.Api/Wallet.Api.csproj src/Wallet.Api/
RUN dotnet restore src/Wallet.Api/Wallet.Api.csproj

COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Wallet.Api/Wallet.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# La imagen escucha en el 8080 (por defecto desde .NET 8) y corre como usuario sin privilegios.
USER $APP_UID
ENTRYPOINT ["dotnet", "Wallet.Api.dll"]
