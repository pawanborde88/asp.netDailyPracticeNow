# ==============================================================================
# Multi-Stage Dockerfile for ASP.NET Core Web API (.NET 10)
# ==============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (utilizes Docker layer caching)
COPY ["dailypracticeASPDotNet.csproj", "./"]
RUN dotnet restore "dailypracticeASPDotNet.csproj"

# Copy the remaining project files
COPY . .

# Build and publish release output
RUN dotnet publish "dailypracticeASPDotNet.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Standard port for modern .NET containers
EXPOSE 8080
EXPOSE 8081

# Configure container environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Copy compiled artifacts from build stage
COPY --from=build /app/publish .

# Set container entrypoint
ENTRYPOINT ["dotnet", "dailypracticeASPDotNet.dll"]
