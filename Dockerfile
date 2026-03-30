# Dockerfile for Aurora Vortex Scalper (C# Engine)
# Wannan file zai bada damar yin deployment a Google Cloud Run ko Compute Engine.

# 1. Build Layer
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY ["Aurora Vortex Scalper.csproj", "./"]
RUN dotnet restore "Aurora Vortex Scalper.csproj"

# Copy the rest of the files and build the app
COPY . .
RUN dotnet publish "Aurora Vortex Scalper.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Layer
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Run the engine
ENTRYPOINT ["dotnet", "Aurora Vortex Scalper.dll"]
