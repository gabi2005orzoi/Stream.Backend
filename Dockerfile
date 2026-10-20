# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY ["Stream.Domain/Stream.Domain.csproj", "Stream.Domain/"]
COPY ["Stream.Application/Stream.Application.csproj", "Stream.Application/"]
COPY ["Stream.Infrastructure/Stream.Infrastructure.csproj", "Stream.Infrastructure/"]
COPY ["Stream.Api/Stream.Api.csproj", "Stream.Api/"]

# Restore dependencies
RUN dotnet restore "Stream.Api/Stream.Api.csproj"

# Copy the rest of the source code
COPY . .

# Publish Release
WORKDIR "/src/Stream.Api"
RUN dotnet publish "Stream.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Stream.Api.dll"]
