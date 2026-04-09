# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY backend/MicroserviceHub.API/*.csproj ./
RUN dotnet restore

COPY backend/MicroserviceHub.API/. ./
RUN dotnet publish -c Release -o /out

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

COPY --from=build /out ./

EXPOSE 80
ENTRYPOINT ["dotnet", "MicroserviceHub.API.dll"]