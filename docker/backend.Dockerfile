# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file using exact directory structure
COPY backend/MicroserviceHub.API/MicroserviceHub.API.csproj ./backend/MicroserviceHub.API/

# Restore dependencies from the folder containing the csproj
WORKDIR /src/backend/MicroserviceHub.API
RUN dotnet restore

# Copy the rest of the source code
COPY backend/MicroserviceHub.API/ ./

# Build and publish the application
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# The aspnet:8.0 image already includes a non-root user named 'app'
USER app

# Copy published output from the build stage
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MicroserviceHub.API.dll"]
