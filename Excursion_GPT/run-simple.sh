#!/bin/bash

# Excursion GPT Simple Run Script
# This script builds the application and runs it with Docker Compose

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored messages
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check prerequisites
check_prerequisites() {
    print_info "Checking prerequisites..."

    # Check if .NET SDK is installed
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed. Please install .NET 9.0 SDK."
        exit 1
    fi

    # Check if Docker is installed
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker."
        exit 1
    fi

    # Check if Docker Compose is available
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        print_error "Docker Compose is not available."
        exit 1
    fi

    print_success "Prerequisites check passed"
}

# Function to build the application
build_application() {
    print_info "Building Excursion GPT application..."

    # Clean previous build
    if [ -d "./publish" ]; then
        print_info "Cleaning previous build..."
        rm -rf ./publish
    fi

    # Build and publish the application
    print_info "Restoring dependencies..."
    dotnet restore

    print_info "Building solution..."
    dotnet build -c Release

    print_info "Publishing application..."
    dotnet publish -c Release -o ./publish

    # Check if publish was successful
    if [ -f "./publish/Excursion_GPT.dll" ]; then
        print_success "Application built successfully"
    else
        print_error "Failed to build application"
        exit 1
    fi
}

# Function to start Docker services
start_services() {
    print_info "Starting Docker services..."

    # Stop any running services first
    if [ "$(docker ps -q --filter name=excursion-gpt)" ]; then
        print_warning "Stopping existing services..."
        docker-compose -f docker-compose.simple.yml down 2>/dev/null || docker compose -f docker-compose.simple.yml down 2>/dev/null
    fi

    # Start services
    if command -v docker-compose &> /dev/null; then
        docker-compose -f docker-compose.simple.yml up -d --build
    else
        docker compose -f docker-compose.simple.yml up -d --build
    fi

    # Wait for services to start
    print_info "Waiting for services to initialize..."
    sleep 10

    print_success "Docker services started"
}

# Function to check service status
check_status() {
    print_info "Checking service status..."

    # Get compose command
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    else
        COMPOSE_CMD="docker compose"
    fi

    echo ""
    print_info "Container Status:"
    echo "=================="
    $COMPOSE_CMD -f docker-compose.simple.yml ps

    echo ""
    print_info "Health Checks:"
    echo "================"

    # Check API health
    if curl -s -f http://localhost:5000/health > /dev/null 2>&1; then
        echo "API:          ✅ Healthy"
    else
        echo "API:          ❌ Unhealthy"
    fi

    # Check PostgreSQL health
    if docker exec excursion-gpt-postgres pg_isready -U postgres > /dev/null 2>&1; then
        echo "PostgreSQL:   ✅ Healthy"
    else
        echo "PostgreSQL:   ❌ Unhealthy"
    fi

    # Check MinIO health
    if curl -s -f http://localhost:9000/minio/health/live > /dev/null 2>&1; then
        echo "MinIO:        ✅ Healthy"
    else
        echo "MinIO:        ❌ Unhealthy"
    fi
}

# Function to show access information
show_access_info() {
    echo ""
    echo "========================================="
    echo "🚀 EXCURSION GPT IS RUNNING!"
    echo "========================================="
    echo ""
    echo "📡 ACCESS POINTS:"
    echo "   API Server:     http://localhost:5000"
    echo "   Swagger UI:     http://localhost:5000/swagger"
    echo "   Health Check:   http://localhost:5000/health"
    echo ""
    echo "🗄️  DATABASE:"
    echo "   PostgreSQL:     localhost:5432"
    echo "   Database:       3D_Excursion"
    echo "   Username:       postgres"
    echo "   Password:       postgres"
    echo ""
    echo "📁 FILE STORAGE:"
    echo "   MinIO Console:  http://localhost:9001"
    echo "   Access Key:     admin"
    echo "   Secret Key:     admin12345"
    echo "   Bucket:         models"
    echo ""
    echo "🔧 MANAGEMENT COMMANDS:"
    echo "   View logs:      docker-compose -f docker-compose.simple.yml logs -f"
    echo "   Stop services:  docker-compose -f docker-compose.simple.yml down"
    echo "   Restart API:    docker-compose -f docker-compose.simple.yml restart api"
    echo ""
    echo "📝 QUICK TESTS:"
    echo "   Test API:       curl http://localhost:5000/health"
    echo "   Check DB:       docker exec -it excursion-gpt-postgres psql -U postgres -d 3D_Excursion"
    echo "   View MinIO:     Open http://localhost:9001 in browser"
    echo ""
    echo "========================================="
}

# Function to stop services
stop_services() {
    print_info "Stopping services..."

    if command -v docker-compose &> /dev/null; then
        docker-compose -f docker-compose.simple.yml down
    else
        docker compose -f docker-compose.simple.yml down
    fi

    print_success "Services stopped"
}

# Function to view logs
view_logs() {
    print_info "Showing logs (Ctrl+C to exit)..."

    if command -v docker-compose &> /dev/null; then
        docker-compose -f docker-compose.simple.yml logs -f
    else
        docker compose -f docker-compose.simple.yml logs -f
    fi
}

# Function to show help
show_help() {
    echo "Excursion GPT Simple Run Script"
    echo "================================"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  build     Build the application only"
    echo "  start     Build and start all services (default)"
    echo "  stop      Stop all services"
    echo "  restart   Restart all services"
    echo "  status    Show service status"
    echo "  logs      View service logs"
    echo "  info      Show access information"
    echo "  help      Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0           # Build and start services"
    echo "  $0 start     # Same as above"
    echo "  $0 stop      # Stop services"
    echo "  $0 status    # Show status"
    echo "  $0 logs      # View logs"
    echo ""
    echo "Services included:"
    echo "  • API (ASP.NET Core 9.0)"
    echo "  • PostgreSQL database"
    echo "  • MinIO file storage"
}

# Main function
main() {
    local command="${1:-start}"

    case "$command" in
        build)
            check_prerequisites
            build_application
            ;;
        start)
            check_prerequisites
            build_application
            start_services
            check_status
            show_access_info
            ;;
        stop)
            stop_services
            ;;
        restart)
            check_prerequisites
            stop_services
            build_application
            start_services
            check_status
            show_access_info
            ;;
        status)
            check_status
            ;;
        logs)
            view_logs
            ;;
        info)
            show_access_info
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            print_error "Unknown command: $command"
            echo "Use '$0 help' for usage information"
            exit 1
            ;;
    esac
}

# Run main function
main "$@"
