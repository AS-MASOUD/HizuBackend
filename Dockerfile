# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY Hizu.sln .
COPY src/Hizu.Domain/Hizu.Domain.csproj src/Hizu.Domain/
COPY src/Hizu.Application/Hizu.Application.csproj src/Hizu.Application/
COPY src/Hizu.Infrastructure/Hizu.Infrastructure.csproj src/Hizu.Infrastructure/
COPY src/Hizu.Api/Hizu.Api.csproj src/Hizu.Api/
COPY tests/Hizu.UnitTests/Hizu.UnitTests.csproj tests/Hizu.UnitTests/

# Restore dependencies
RUN dotnet restore Hizu.sln

# Copy all sources and build
COPY . .
WORKDIR /src/src/Hizu.Api
RUN dotnet publish Hizu.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/wwwroot/uploads \
    && chown -R app:app /app/wwwroot

# Non-root user for security
USER app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Hizu.Api.dll"]

