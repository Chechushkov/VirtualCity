# Simple Dockerfile that copies pre-built application
# This avoids NuGet restore issues in Docker

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser:appuser /app
USER appuser

# Copy published application from local build
COPY Excursion_GPT/published-app/ .

# Copy buildings.json data file
COPY buildings.json ./buildings.json

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Expose port
EXPOSE 80

# Entry point
ENTRYPOINT ["dotnet", "Excursion_GPT.dll"]
