# Use the official .NET 7 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy all project files
COPY GameOfLife.API/GameOfLife.API.csproj GameOfLife.API/
COPY GameOfLife.Core/GameOfLife.Service.csproj GameOfLife.Core/
COPY GameOfLife.Infrastructure/GameOfLife.Repository.csproj GameOfLife.Infrastructure/
COPY GameOfLife.Models/GameOfLife.Models.csproj GameOfLife.Models/

# Restore dependencies
RUN dotnet restore GameOfLife.API/GameOfLife.API.csproj

# Copy all source code
COPY . .

# Build the application
WORKDIR /src/GameOfLife.API
RUN dotnet publish -c Release -o /app/publish

# Use the official ASP.NET runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# Copy the built application from the build image
COPY --from=build /app/publish .

# Expose port 80 for HTTP traffic
EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "GameOfLife.API.dll"]
