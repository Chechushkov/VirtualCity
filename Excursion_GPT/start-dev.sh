#!/bin/bash

# Excursion GPT Development Quick Start Script
# This script provides a simple way to start the development environment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
COMPOSE_FILE="docker-compose.dev.yml"
PROJECT_NAME="excursion-gpt-dev"

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

# Function to check if Docker is installed
check_docker() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker first."
        echo "Visit: https://docs.docker.com/get-docker/"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        # Check for Docker Compose plugin
        if ! docker compose version &> /dev/null; then
            print_error "Docker Compose is not installed. Please install Docker Compose."
            echo "Visit: https://docs.docker.com/compose/install/"
            exit 1
        fi
        COMPOSE_CMD="docker compose"
    else
        COMPOSE_CMD="docker-compose"
    fi

    print_success "Docker and Docker Compose are available"
}

# Function to start development environment
start_development() {
    print_info "Starting Excursion GPT development environment..."
    echo ""

    # Check if services are already running
    if [ "$($COMPOSE_CMD -f $COMPOSE_FILE ps -q | wc -l)" -gt 0 ]; then
        print_warning "Services are already running. Restarting..."
        $COMPOSE_CMD -f $COMPOSE_FILE down
    fi

    # Build and start services
    print_info "Building and starting services..."
    $COMPOSE_CMD -f $COMPOSE_FILE up -d --build

    # Wait for services to be ready
    print_info "Waiting for services to be ready..."
    sleep 10

    # Check service status
    print_info "Checking service status..."
    $COMPOSE_CMD -f $COMPOSE_FILE ps

    echo ""
    print_success "Development environment started successfully!"
    echo ""

    show_access_info
}

# Function to show access information
show_access_info() {
    echo "========================================="
    echo "🚀 EXCURSION GPT DEVELOPMENT ENVIRONMENT"
    echo "========================================="
    echo ""
    echo "📡 API Endpoints:"
    echo "   API Server:     http://localhost:5000"
    echo "   Swagger UI:     http://localhost:5000/swagger"
    echo "   Health Check:   http://localhost:5000/health"
    echo ""
    echo "🗄️  Database:"
    echo "   PostgreSQL:     localhost:5432"
    echo "   Database:       3D_Excursion"
    echo "   Username:       postgres"
    echo "   Password:       postgres"
    echo ""
    echo "📁 File Storage:"
    echo "   MinIO Console:  http://localhost:9001"
    echo "   Access Key:     admin"
    echo "   Secret Key:     admin12345"
    echo "   Bucket:         models"
    echo ""
    echo "🔧 Management Commands:"
    echo "   View logs:      docker-compose -f $COMPOSE_FILE logs -f"
    echo "   Stop services:  docker-compose -f $COMPOSE_FILE down"
    echo "   Restart API:    docker-compose -f $COMPOSE_FILE restart api"
    echo ""
    echo "📝 Useful Commands:"
    echo "   Test API:       curl http://localhost:5000/health"
    echo "   Check DB:       docker exec -it excursion-gpt-postgres-dev psql -U postgres -d 3D_Excursion"
    echo "   View MinIO:     Open http://localhost:9001 in browser"
    echo ""
    echo "========================================="
    echo ""

    print_info "To view logs, run: docker-compose -f $COMPOSE_FILE logs -f"
    print_info "To stop services, run: docker-compose -f $COMPOSE_FILE down"
}

# Function to stop development environment
stop_development() {
    print_info "Stopping development environment..."
    $COMPOSE_CMD -f $COMPOSE_FILE down
    print_success "Development environment stopped"
}

# Function to view logs
view_logs() {
    print_info "Showing logs (Ctrl+C to exit)..."
    $COMPOSE_CMD -f $COMPOSE_FILE logs -f
}

# Function to restart services
restart_services() {
    print_info "Restarting services..."
    $COMPOSE_CMD -f $COMPOSE_FILE restart
    print_success "Services restarted"
}

# Function to rebuild application
rebuild_app() {
    print_info "Rebuilding application..."
    $COMPOSE_CMD -f $COMPOSE_FILE build api
    print_success "Application rebuilt"
}

# Function to run database migrations
run_migrations() {
    print_info "Running database migrations..."
    $COMPOSE_CMD -f $COMPOSE_FILE exec api dotnet ef database update
    print_success "Database migrations completed"
}

# Function to show help
show_help() {
    echo "Excursion GPT Development Quick Start Script"
    echo "============================================"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start       Start development environment (default)"
    echo "  stop        Stop development environment"
    echo "  restart     Restart all services"
    echo "  logs        View logs for all services"
    echo "  rebuild     Rebuild the application"
    echo "  migrate     Run database migrations"
    echo "  status      Show service status"
    echo "  info        Show access information"
    echo "  help        Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0           # Start development environment"
    echo "  $0 start     # Same as above"
    echo "  $0 logs      # View logs"
    echo "  $0 stop      # Stop services"
    echo ""
    echo "Environment:"
    echo "  Uses: $COMPOSE_FILE"
    echo "  Project: $PROJECT_NAME"
}

# Function to show status
show_status() {
    print_info "Service Status:"
    echo ""
    $COMPOSE_CMD -f $COMPOSE_FILE ps
    echo ""

    # Check health endpoints
    print_info "Health Checks:"
    if curl -s http://localhost:5000/health > /dev/null; then
        echo "  API:          ✅ Healthy"
    else
        echo "  API:          ❌ Unhealthy"
    fi

    if docker exec excursion-gpt-postgres-dev pg_isready -U postgres > /dev/null; then
        echo "  PostgreSQL:   ✅ Healthy"
    else
        echo "  PostgreSQL:   ❌ Unhealthy"
    fi

    if curl -s http://localhost:9000/minio/health/live > /dev/null; then
        echo "  MinIO:        ✅ Healthy"
    else
        echo "  MinIO:        ❌ Unhealthy"
    fi
}

# Main script execution
main() {
    check_docker

    local command=${1:-"start"}

    case "$command" in
        start)
            start_development
            ;;
        stop)
            stop_development
            ;;
        restart)
            restart_services
            ;;
        logs)
            view_logs
            ;;
        rebuild)
            rebuild_app
            ;;
        migrate)
            run_migrations
            ;;
        status)
            show_status
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

# Run main function with all arguments
main "$@"
