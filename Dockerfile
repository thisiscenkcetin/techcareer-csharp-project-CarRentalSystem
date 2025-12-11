# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CarRentalWeb/CarRentalWeb.csproj CarRentalWeb/
RUN dotnet restore CarRentalWeb/CarRentalWeb.csproj

# Copy everything else and build
COPY CarRentalWeb/ CarRentalWeb/
WORKDIR /src/CarRentalWeb
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (Render will set PORT env variable)
EXPOSE 5189

# Run the app
ENTRYPOINT ["dotnet", "CarRentalWeb.dll"]
