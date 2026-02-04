# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY Excursion_GPT/Excursion_GPT.sln ./

# Copy all project files
COPY Excursion_GPT/Excursion_GPT.csproj ./Excursion_GPT/
COPY Excursion_GPT.Application/Excursion_GPT.Application.csproj ./Excursion_GPT.Application/
COPY Excursion_GPT.Domain/Excursion_GPT.Domain.csproj ./Excursion_GPT.Domain/
COPY Excursion_GPT.Infrastructure/Excursion_GPT.Infrastructure.csproj ./Excursion_GPT.Infrastructure/
COPY Excursion_GPT.Tests/Excursion_GPT.Tests.csproj ./Excursion_GPT.Tests/

# Restore dependencies
WORKDIR /src/Excursion_GPT
RUN dotnet restore

# Copy all source code
WORKDIR /src
COPY Excursion_GPT/ ./Excursion_GPT/
COPY Excursion_GPT.Application/ ./Excursion_GPT.Application/
COPY Excursion_GPT.Domain/ ./Excursion_GPT.Domain/
COPY Excursion_GPT.Infrastructure/ ./Excursion_GPT.Infrastructure/
COPY Excursion_GPT.Tests/ ./Excursion_GPT.Tests/

# Build the application
WORKDIR /src/Excursion_GPT
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser:appuser /app
USER appuser

# Copy published application
COPY --from=publish --chown=appuser:appuser /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Expose port
EXPOSE 80

# Entry point
ENTRYPOINT ["dotnet", "Excursion_GPT.dll"]
