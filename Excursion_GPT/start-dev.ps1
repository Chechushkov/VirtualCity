# Excursion GPT Windows Development Startup Script
# PowerShell script for easy Docker Compose management on Windows

param(
    [string]$Command = "start",
    [string[]]$Services = @()
)

# Configuration
$ComposeFile = "docker-compose.dev.yml"
$ProjectName = "excursion-gpt-dev"
$EnvFile = "env.config"

# Colors for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$WarningColor = "Yellow"
$InfoColor = "Cyan"

# Function to print colored messages
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor $SuccessColor
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor $WarningColor
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $ErrorColor
}

# Function to check if Docker is running
function Test-DockerRunning {
    try {
        $dockerInfo = docker info 2>&1
        if ($LASTEXITCODE -eq 0) {
            return $true
        }
        return $false
    }
    catch {
        return $false
    }
}

# Function to start Docker Desktop
function Start-DockerDesktop {
    Write-Info "Starting Docker Desktop..."

    # Try to start Docker Desktop
    try {
        Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe" -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 5

        # Wait for Docker to start (up to 60 seconds)
        $timeout = 60
        $startTime = Get-Date

        while ((Get-Date) -lt $startTime.AddSeconds($timeout)) {
            if (Test-DockerRunning) {
                Write-Success "Docker Desktop started successfully"
                return $true
            }
            Write-Host "." -NoNewline -ForegroundColor $InfoColor
            Start-Sleep -Seconds 2
        }

        Write-Error "Docker Desktop failed to start within $timeout seconds"
        return $false
    }
    catch {
        Write-Error "Failed to start Docker Desktop: $_"
        return $false
    }
}

# Function to check prerequisites
function Test-Prerequisites {
    Write-Info "Checking prerequisites..."

    # Check if Docker is installed
    try {
        $dockerVersion = docker --version
        Write-Success "Docker installed: $dockerVersion"
    }
    catch {
        Write-Error "Docker is not installed. Please install Docker Desktop for Windows."
        Write-Host "Download from: https://www.docker.com/products/docker-desktop/" -ForegroundColor $InfoColor
        return $false
    }

    # Check if Docker Compose is available
    try {
        if (docker compose version) {
            Write-Success "Docker Compose plugin available"
            $script:ComposeCommand = "docker compose"
        }
        elseif (docker-compose --version) {
            Write-Success "Docker Compose standalone available"
            $script:ComposeCommand = "docker-compose"
        }
        else {
            Write-Error "Docker Compose is not available"
            return $false
        }
    }
    catch {
        Write-Error "Docker Compose check failed: $_"
        return $false
    }

    # Check if Docker is running
    if (-not (Test-DockerRunning)) {
        Write-Warning "Docker is not running"
        if (-not (Start-DockerDesktop)) {
            return $false
        }
    }

    return $true
}

# Function to start development environment
function Start-Development {
    Write-Info "Starting Excursion GPT development environment..."

    # Check if services are already running
    $runningContainers = & docker ps -q --filter "name=excursion-gpt"
    if ($runningContainers) {
        Write-Warning "Services are already running. Stopping first..."
        Stop-Development
    }

    # Build and start services
    Write-Info "Building and starting services..."

    $composeArgs = @("-f", $ComposeFile, "up", "-d", "--build")
    if ($Services.Count -gt 0) {
        $composeArgs += $Services
    }

    Invoke-Expression "$ComposeCommand $composeArgs"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to start services"
        return $false
    }

    # Wait for services to initialize
    Write-Info "Waiting for services to initialize..."
    Start-Sleep -Seconds 10

    # Show status
    Show-Status

    Write-Success "Development environment started successfully!"
    Show-AccessInfo

    return $true
}

# Function to stop development environment
function Stop-Development {
    Write-Info "Stopping development environment..."

    Invoke-Expression "$ComposeCommand -f $ComposeFile down"

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Development environment stopped"
        return $true
    }
    else {
        Write-Error "Failed to stop services"
        return $false
    }
}

# Function to restart services
function Restart-Services {
    param([string[]]$ServiceNames = @())

    if ($ServiceNames.Count -eq 0) {
        Write-Info "Restarting all services..."
        Invoke-Expression "$ComposeCommand -f $ComposeFile restart"
    }
    else {
        Write-Info "Restarting services: $($ServiceNames -join ', ')"
        $restartArgs = @("-f", $ComposeFile, "restart") + $ServiceNames
        Invoke-Expression "$ComposeCommand $restartArgs"
    }

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Services restarted"
        return $true
    }
    else {
        Write-Error "Failed to restart services"
        return $false
    }
}

# Function to view logs
function View-Logs {
    param([string[]]$ServiceNames = @())

    $logArgs = @("-f", $ComposeFile, "logs", "-f")
    if ($ServiceNames.Count -gt 0) {
        $logArgs += $ServiceNames
    }

    Write-Info "Showing logs (Ctrl+C to exit)..."
    Invoke-Expression "$ComposeCommand $logArgs"
}

# Function to show status
function Show-Status {
    Write-Info "Service Status:"
    Write-Host "=========================================" -ForegroundColor $InfoColor

    Invoke-Expression "$ComposeCommand -f $ComposeFile ps"

    Write-Host "`nHealth Checks:" -ForegroundColor $InfoColor

    # Check API health
    try {
        $healthResponse = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 5 -ErrorAction SilentlyContinue
        Write-Host "  API:          ✅ Healthy" -ForegroundColor $SuccessColor
    }
    catch {
        Write-Host "  API:          ❌ Unhealthy" -ForegroundColor $ErrorColor
    }

    # Check PostgreSQL health
    try {
        $postgresHealth = docker exec excursion-gpt-postgres-dev pg_isready -U postgres 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  PostgreSQL:   ✅ Healthy" -ForegroundColor $SuccessColor
        }
        else {
            Write-Host "  PostgreSQL:   ❌ Unhealthy" -ForegroundColor $ErrorColor
        }
    }
    catch {
        Write-Host "  PostgreSQL:   ❌ Unavailable" -ForegroundColor $ErrorColor
    }

    # Check MinIO health
    try {
        $minioHealth = Invoke-RestMethod -Uri "http://localhost:9000/minio/health/live" -TimeoutSec 5 -ErrorAction SilentlyContinue
        Write-Host "  MinIO:        ✅ Healthy" -ForegroundColor $SuccessColor
    }
    catch {
        Write-Host "  MinIO:        ❌ Unhealthy" -ForegroundColor $ErrorColor
    }
}

# Function to show access information
function Show-AccessInfo {
    Write-Host "`n=========================================" -ForegroundColor $SuccessColor
    Write-Host "🚀 EXCURSION GPT DEVELOPMENT ENVIRONMENT" -ForegroundColor $SuccessColor
    Write-Host "=========================================" -ForegroundColor $SuccessColor
    Write-Host ""
    Write-Host "📡 API Endpoints:" -ForegroundColor $InfoColor
    Write-Host "   API Server:     http://localhost:5000" -ForegroundColor $WarningColor
    Write-Host "   Swagger UI:     http://localhost:5000/swagger" -ForegroundColor $WarningColor
    Write-Host "   Health Check:   http://localhost:5000/health" -ForegroundColor $WarningColor
    Write-Host ""
    Write-Host "🗄️  Database:" -ForegroundColor $InfoColor
    Write-Host "   PostgreSQL:     localhost:5432" -ForegroundColor $WarningColor
    Write-Host "   Database:       3D_Excursion" -ForegroundColor $WarningColor
    Write-Host "   Username:       postgres" -ForegroundColor $WarningColor
    Write-Host "   Password:       postgres" -ForegroundColor $WarningColor
    Write-Host ""
    Write-Host "📁 File Storage:" -ForegroundColor $InfoColor
    Write-Host "   MinIO Console:  http://localhost:9001" -ForegroundColor $WarningColor
    Write-Host "   Access Key:     admin" -ForegroundColor $WarningColor
    Write-Host "   Secret Key:     admin12345" -ForegroundColor $WarningColor
    Write-Host "   Bucket:         models" -ForegroundColor $WarningColor
    Write-Host ""
    Write-Host "🔧 Management Commands:" -ForegroundColor $InfoColor
    Write-Host "   View logs:      .\start-dev.ps1 logs" -ForegroundColor $WarningColor
    Write-Host "   Stop services:  .\start-dev.ps1 stop" -ForegroundColor $WarningColor
    Write-Host "   Restart:        .\start-dev.ps1 restart" -ForegroundColor $WarningColor
    Write-Host "   Status:         .\start-dev.ps1 status" -ForegroundColor $WarningColor
    Write-Host ""
    Write-Host "📝 Useful Commands:" -ForegroundColor $InfoColor
    Write-Host "   Test API:       curl http://localhost:5000/health" -ForegroundColor $WarningColor
    Write-Host "   Check DB:       docker exec -it excursion-gpt-postgres-dev psql -U postgres -d 3D_Excursion" -ForegroundColor $WarningColor
    Write-Host "   View MinIO:     Open http://localhost:9001 in browser" -ForegroundColor $WarningColor
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor $SuccessColor
    Write-Host ""
}

# Function to rebuild application
function Rebuild-Application {
    Write-Info "Rebuilding application..."

    Invoke-Expression "$ComposeCommand -f $ComposeFile build api"

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Application rebuilt"
        return $true
    }
    else {
        Write-Error "Failed to rebuild application"
        return $false
    }
}

# Function to run database migrations
function Run-Migrations {
    Write-Info "Running database migrations..."

    try {
        Invoke-Expression "$ComposeCommand -f $ComposeFile exec api dotnet ef database update"

        if ($LASTEXITCODE -eq 0) {
            Write-Success "Database migrations completed"
            return $true
        }
        else {
            Write-Error "Database migrations failed"
            return $false
        }
    }
    catch {
        Write-Error "Failed to run migrations: $_"
        return $false
    }
}

# Function to show help
function Show-Help {
    Write-Host "Excursion GPT Windows Development Script" -ForegroundColor $SuccessColor
    Write-Host "========================================" -ForegroundColor $SuccessColor
    Write-Host ""
    Write-Host "Usage: .\start-dev.ps1 [command] [services...]" -ForegroundColor $InfoColor
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor $InfoColor
    Write-Host "  start       Start development environment (default)" -ForegroundColor $WarningColor
    Write-Host "  stop        Stop development environment" -ForegroundColor $WarningColor
    Write-Host "  restart     Restart all services or specific services" -ForegroundColor $WarningColor
    Write-Host "  logs        View logs for all services or specific services" -ForegroundColor $WarningColor
    Write-Host "  rebuild     Rebuild the application" -ForegroundColor $WarningColor
    Write-Host "  migrate     Run database migrations" -ForegroundColor $WarningColor
    Write-Host "  status      Show service status and health checks" -ForegroundColor $WarningColor
    Write-Host "  info        Show access information" -ForegroundColor $WarningColor
    Write-Host "  help        Show this help message" -ForegroundColor $WarningColor
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor $InfoColor
    Write-Host "  .\start-dev.ps1                    # Start all services" -ForegroundColor $WarningColor
    Write-Host "  .\start-dev.ps1 start             # Same as above" -ForegroundColor $WarningColor
    Write-Host "  .\start-dev.ps1 stop              # Stop all services" -ForegroundColor $WarningColor
    Write-Host "  .\start-dev.ps1 restart api       # Restart only API service" -ForegroundColor $WarningColor
    Write-Host "  .\start-dev.ps1 logs api postgres # View API and PostgreSQL logs" -ForegroundColor $WarningColor
    Write-Host "  .\start-dev.ps1 status            # Show service status" -ForegroundColor $WarningColor
    Write-Host "  .\start-dev.ps1 migrate           # Run database migrations" -ForegroundColor $WarningColor
    Write-Host ""
    Write-Host "Available services: postgres, minio, api" -ForegroundColor $InfoColor
    Write-Host ""
    Write-Host "Configuration:" -ForegroundColor $InfoColor
    Write-Host "  Compose file: $ComposeFile" -ForegroundColor $WarningColor
    Write-Host "  Project name: $ProjectName" -ForegroundColor $WarningColor
    Write-Host "  Environment:  $EnvFile" -ForegroundColor $WarningColor
}

# Main script execution
function Main {
    # Check prerequisites
    if (-not (Test-Prerequisites)) {
        Write-Error "Prerequisites check failed. Exiting."
        exit 1
    }

    # Process command
    switch ($Command.ToLower()) {
        "start" {
            Start-Development
            break
        }
        "stop" {
            Stop-Development
            break
        }
        "restart" {
            if ($Services.Count -eq 0) {
                Restart-Services
            }
            else {
                Restart-Services -ServiceNames $Services
            }
            break
        }
        "logs" {
            View-Logs -ServiceNames $Services
            break
        }
        "rebuild" {
            Rebuild-Application
            break
        }
        "migrate" {
            Run-Migrations
            break
        }
        "status" {
            Show-Status
            break
        }
        "info" {
            Show-AccessInfo
            break
        }
        "help" {
            Show-Help
            break
        }
        default {
            Write-Error "Unknown command: $Command"
            Write-Host "Use '.\start-dev.ps1 help' for usage information" -ForegroundColor $InfoColor
            exit 1
        }
    }
}

# Run main function
Main
