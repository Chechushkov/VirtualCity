#!/bin/bash

# Excursion GPT Docker Build Script
# This script prepares the project for Docker build by copying all necessary files

set -e

echo "🚀 Preparing Excursion GPT for Docker build..."

# Create temporary directory for Docker build
BUILD_DIR="docker-build"
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

echo "📁 Creating build structure..."

# Copy main project
echo "📦 Copying main project..."
cp -r . "$BUILD_DIR/"

# Copy referenced projects
echo "📦 Copying Excursion_GPT.Application..."
cp -r ../Excursion_GPT.Application "$BUILD_DIR/"

echo "📦 Copying Excursion_GPT.Domain..."
cp -r ../Excursion_GPT.Domain "$BUILD_DIR/"

echo "📦 Copying Excursion_GPT.Infrastructure..."
cp -r ../Excursion_GPT.Infrastructure "$BUILD_DIR/"

# Create a Dockerfile that works with the copied structure
echo "🐳 Creating Dockerfile..."
cat > "$BUILD_DIR/Dockerfile" << 'EOF'
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files
COPY Excursion_GPT.csproj ./Excursion_GPT/
COPY Excursion_GPT.Application/Excursion_GPT.Application.csproj ./Excursion_GPT.Application/
COPY Excursion_GPT.Domain/Excursion_GPT.Domain.csproj ./Excursion_GPT.Domain/
COPY Excursion_GPT.Infrastructure/Excursion_GPT.Infrastructure.csproj ./Excursion_GPT.Infrastructure/

# Restore dependencies
WORKDIR /src/Excursion_GPT
RUN dotnet restore

# Copy everything else
WORKDIR /src
COPY . .

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
EOF

echo "✅ Build preparation complete!"
echo ""
echo "To build and run with Docker Compose:"
echo "cd $BUILD_DIR && docker-compose -f docker-compose.simple.yml up -d --build"
echo ""
echo "Or build the image directly:"
echo "cd $BUILD_DIR && docker build -t excursion-gpt-api ."
