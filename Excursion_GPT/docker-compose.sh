#!/bin/bash

# Docker Compose Management Script for Excursion GPT
# This script provides easy management of the Docker Compose setup

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
COMPOSE_FILE="docker-compose.yml"
PROJECT_NAME="excursion-gpt"
ENV_FILE="env.config"

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
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        # Check for Docker Compose plugin
        if ! docker compose version &> /dev/null; then
            print_error "Docker Compose is not installed. Please install Docker Compose."
            exit 1
        fi
        COMPOSE_CMD="docker compose"
    else
        COMPOSE_CMD="docker-compose"
    fi

    print_success "Docker and Docker Compose are available"
}

# Function to build the application
build_app() {
    print_info "Building the application..."
    $COMPOSE_CMD -f $COMPOSE_FILE build api
    print_success "Application built successfully"
}

# Function to start all services
start_all() {
    print_info "Starting all services..."
    $COMPOSE_CMD -f $COMPOSE_FILE --env-file $ENV_FILE up -d
    print_success "All services started successfully"

    # Show service status
    show_status
}

# Function to start specific services
start_services() {
    if [ $# -eq 0 ]; then
        print_error "Please specify services to start"
        echo "Usage: $0 start [service1] [service2] ..."
        exit 1
    fi

    print_info "Starting services: $@"
    $COMPOSE_CMD -f $COMPOSE_FILE --env-file $ENV_FILE up -d "$@"
    print_success "Services started: $@"
}

# Function to stop all services
stop_all() {
    print_info "Stopping all services..."
    $COMPOSE_CMD -f $COMPOSE_FILE down
    print_success "All services stopped"
}

# Function to stop specific services
stop_services() {
    if [ $# -eq 0 ]; then
        print_error "Please specify services to stop"
        echo "Usage: $0 stop [service1] [service2] ..."
        exit 1
    fi

    print_info "Stopping services: $@"
    $COMPOSE_CMD -f $COMPOSE_FILE stop "$@"
    print_success "Services stopped: $@"
}

# Function to restart services
restart_services() {
    if [ $# -eq 0 ]; then
        print_info "Restarting all services..."
        $COMPOSE_CMD -f $COMPOSE_FILE --env-file $ENV_FILE restart
        print_success "All services restarted"
    else
        print_info "Restarting services: $@"
        $COMPOSE_CMD -f $COMPOSE_FILE restart "$@"
        print_success "Services restarted: $@"
    fi
}

# Function to show service status
show_status() {
    print_info "Service Status:"
    echo "========================================="
    $COMPOSE_CMD -f $COMPOSE_FILE ps

    echo ""
    print_info "Service URLs:"
    echo "========================================="
    echo "API:              http://localhost:5000"
    echo "Swagger UI:       http://localhost:5000/swagger"
    echo "Health Check:     http://localhost:5000/health"
    echo "MinIO Console:    http://localhost:9001"
    echo "pgAdmin:          http://localhost:5050"
    echo "PostgreSQL:       localhost:5432"
    echo ""
    echo "MinIO Credentials:"
    echo "  Access Key:     admin"
    echo "  Secret Key:     admin12345"
    echo ""
    echo "pgAdmin Credentials:"
    echo "  Email:          admin@excursion.com"
    echo "  Password:       admin123"
    echo "========================================="
}

# Function to view logs
view_logs() {
    if [ $# -eq 0 ]; then
        print_info "Showing logs for all services (Ctrl+C to exit)..."
        $COMPOSE_CMD -f $COMPOSE_FILE logs -f
    else
        print_info "Showing logs for services: $@ (Ctrl+C to exit)..."
        $COMPOSE_CMD -f $COMPOSE_FILE logs -f "$@"
    fi
}

# Function to rebuild and restart
rebuild() {
    print_info "Rebuilding and restarting services..."
    $COMPOSE_CMD -f $COMPOSE_FILE --env-file $ENV_FILE up -d --build
    print_success "Services rebuilt and restarted"
}

# Function to remove everything
clean() {
    print_warning "This will remove all containers, volumes, and networks!"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Removing all containers, volumes, and networks..."
        $COMPOSE_CMD -f $COMPOSE_FILE down -v --rmi all
        print_success "All Docker resources removed"
    else
        print_info "Clean operation cancelled"
    fi
}

# Function to run database migrations
run_migrations() {
    print_info "Running database migrations..."
    $COMPOSE_CMD -f $COMPOSE_FILE exec api dotnet ef database update
    print_success "Database migrations completed"
}

# Function to create a database backup
backup_database() {
    local timestamp=$(date +%Y%m%d_%H%M%S)
    local backup_file="backup_${timestamp}.sql"

    print_info "Creating database backup: $backup_file"

    # Create backup directory if it doesn't exist
    mkdir -p backups

    # Create backup
    $COMPOSE_CMD -f $COMPOSE_FILE exec postgres pg_dump -U postgres 3D_Excursion > "backups/$backup_file"

    if [ $? -eq 0 ]; then
        print_success "Database backup created: backups/$backup_file"
        echo "Backup size: $(du -h "backups/$backup_file" | cut -f1)"
    else
        print_error "Failed to create database backup"
        exit 1
    fi
}

# Function to restore database from backup
restore_database() {
    if [ $# -eq 0 ]; then
        print_error "Please specify backup file to restore"
        echo "Available backups:"
        ls -la backups/*.sql 2>/dev/null || echo "No backups found"
        exit 1
    fi

    local backup_file=$1

    if [ ! -f "$backup_file" ]; then
        print_error "Backup file not found: $backup_file"
        exit 1
    fi

    print_warning "This will overwrite the current database!"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Restoring database from: $backup_file"

        # Drop and recreate database
        $COMPOSE_CMD -f $COMPOSE_FILE exec postgres psql -U postgres -c "DROP DATABASE IF EXISTS \"3D_Excursion\";"
        $COMPOSE_CMD -f $COMPOSE_FILE exec postgres psql -U postgres -c "CREATE DATABASE \"3D_Excursion\";"

        # Restore from backup
        $COMPOSE_CMD -f $COMPOSE_FILE exec -T postgres psql -U postgres 3D_Excursion < "$backup_file"

        if [ $? -eq 0 ]; then
            print_success "Database restored successfully"
        else
            print_error "Failed to restore database"
            exit 1
        fi
    else
        print_info "Restore operation cancelled"
    fi
}

# Function to show help
show_help() {
    echo "Excursion GPT Docker Compose Management Script"
    echo "=============================================="
    echo ""
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  build                     Build the application"
    echo "  start                     Start all services"
    echo "  start [service...]        Start specific services"
    echo "  stop                      Stop all services"
    echo "  stop [service...]         Stop specific services"
    echo "  restart [service...]      Restart services (all if none specified)"
    echo "  status                    Show service status and URLs"
    echo "  logs [service...]         View logs (all if none specified)"
    echo "  rebuild                   Rebuild and restart all services"
    echo "  clean                     Remove all containers, volumes, and networks"
    echo "  migrate                   Run database migrations"
    echo "  backup                    Create database backup"
    echo "  restore <backup_file>     Restore database from backup"
    echo "  help                      Show this help message"
    echo ""
    echo "Available services:"
    echo "  postgres, minio, api, nginx, pgadmin"
    echo ""
    echo "Examples:"
    echo "  $0 start                  # Start all services"
    echo "  $0 start postgres minio   # Start only database and MinIO"
    echo "  $0 logs api               # View API logs"
    echo "  $0 migrate                # Run database migrations"
    echo "  $0 backup                 # Create database backup"
}

# Main script execution
main() {
    check_docker

    case "$1" in
        build)
            build_app
            ;;
        start)
            shift
            if [ $# -eq 0 ]; then
                start_all
            else
                start_services "$@"
            fi
            ;;
        stop)
            shift
            if [ $# -eq 0 ]; then
                stop_all
            else
                stop_services "$@"
            fi
            ;;
        restart)
            shift
            restart_services "$@"
            ;;
        status)
            show_status
            ;;
        logs)
            shift
            view_logs "$@"
            ;;
        rebuild)
            rebuild
            ;;
        clean)
            clean
            ;;
        migrate)
            run_migrations
            ;;
        backup)
            backup_database
            ;;
        restore)
            shift
            restore_database "$@"
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            if [ $# -eq 0 ]; then
                show_help
            else
                print_error "Unknown command: $1"
                echo "Use '$0 help' for usage information"
                exit 1
            fi
            ;;
    esac
}

# Run main function with all arguments
main "$@"
