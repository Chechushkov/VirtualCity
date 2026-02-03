#!/bin/bash

# Excursion GPT Production Management Script
# This script provides management commands for the production environment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
COMPOSE_FILE="docker-compose.prod.yml"
ENV_FILE=".env.production"
PROJECT_NAME="excursion-gpt-prod"

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

    # Check if Docker is installed
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        exit 1
    fi

    # Check if Docker Compose is available
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        print_error "Docker Compose is not available"
        exit 1
    fi

    # Check if environment file exists
    if [ ! -f "$ENV_FILE" ]; then
        print_error "Environment file not found: $ENV_FILE"
        print_info "Create it from the template: cp env.production.example $ENV_FILE"
        exit 1
    fi

    print_success "Prerequisites check passed"
}

# Function to get compose command
get_compose_cmd() {
    if command -v docker-compose &> /dev/null; then
        echo "docker-compose"
    else
        echo "docker compose"
    fi
}

# Function to start production environment
start_production() {
    check_prerequisites
    local compose_cmd=$(get_compose_cmd)

    print_info "Starting production environment..."

    # Build and start services
    $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE up -d --build

    print_success "Production environment started"
    show_status
}

# Function to stop production environment
stop_production() {
    local compose_cmd=$(get_compose_cmd)

    print_info "Stopping production environment..."
    $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE down
    print_success "Production environment stopped"
}

# Function to restart production environment
restart_production() {
    local compose_cmd=$(get_compose_cmd)

    print_info "Restarting production environment..."
    $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE restart
    print_success "Production environment restarted"
}

# Function to show status
show_status() {
    local compose_cmd=$(get_compose_cmd)

    print_info "Production Environment Status:"
    echo "========================================="
    $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE ps

    echo ""
    print_info "Service URLs:"
    echo "========================================="
    echo "API:              http://localhost:5000"
    echo "API Health:       http://localhost:5000/health"
    echo "MinIO Console:    http://localhost:9001"
    echo "Prometheus:       http://localhost:9090"
    echo "Grafana:          http://localhost:3000"
    echo "PostgreSQL:       localhost:5432"
    echo "Redis:            localhost:6379"
    echo ""

    print_info "Health Checks:"
    echo "========================================="

    # Check API health
    if curl -s -f http://localhost:5000/health > /dev/null 2>&1; then
        echo "API:          ✅ Healthy"
    else
        echo "API:          ❌ Unhealthy"
    fi

    # Check PostgreSQL health
    if docker exec excursion-gpt-postgres-prod pg_isready -U postgres > /dev/null 2>&1; then
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

    # Check Redis health
    if docker exec excursion-gpt-redis-prod redis-cli ping > /dev/null 2>&1; then
        echo "Redis:        ✅ Healthy"
    else
        echo "Redis:        ❌ Unhealthy"
    fi
}

# Function to view logs
view_logs() {
    local compose_cmd=$(get_compose_cmd)
    local service="$1"

    if [ -z "$service" ]; then
        print_info "Showing logs for all services (Ctrl+C to exit)..."
        $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE logs -f
    else
        print_info "Showing logs for $service (Ctrl+C to exit)..."
        $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE logs -f "$service"
    fi
}

# Function to backup database
backup_database() {
    local timestamp=$(date +%Y%m%d_%H%M%S)
    local backup_dir="backups"
    local backup_file="$backup_dir/backup_$timestamp.sql"

    print_info "Creating database backup..."

    # Create backup directory if it doesn't exist
    mkdir -p "$backup_dir"

    # Read database password from environment file
    local db_password=$(grep POSTGRES_PASSWORD "$ENV_FILE" | cut -d'=' -f2)

    # Create backup
    docker exec excursion-gpt-postgres-prod pg_dump -U postgres 3D_Excursion > "$backup_file"

    if [ $? -eq 0 ]; then
        local file_size=$(du -h "$backup_file" | cut -f1)
        print_success "Database backup created: $backup_file ($file_size)"

        # Clean up old backups (keep last 30 days)
        find "$backup_dir" -name "backup_*.sql" -mtime +30 -delete
        print_info "Cleaned up backups older than 30 days"
    else
        print_error "Failed to create database backup"
        exit 1
    fi
}

# Function to restore database
restore_database() {
    local backup_file="$1"

    if [ -z "$backup_file" ]; then
        print_error "Please specify backup file to restore"
        echo "Available backups:"
        ls -la backups/*.sql 2>/dev/null || echo "No backups found"
        exit 1
    fi

    if [ ! -f "$backup_file" ]; then
        print_error "Backup file not found: $backup_file"
        exit 1
    fi

    print_warning "This will overwrite the current production database!"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_info "Restore operation cancelled"
        return
    fi

    print_info "Restoring database from: $backup_file"

    # Stop API service to prevent connections during restore
    local compose_cmd=$(get_compose_cmd)
    $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE stop api

    # Drop and recreate database
    docker exec excursion-gpt-postgres-prod psql -U postgres -c "DROP DATABASE IF EXISTS \"3D_Excursion\";"
    docker exec excursion-gpt-postgres-prod psql -U postgres -c "CREATE DATABASE \"3D_Excursion\";"

    # Restore from backup
    docker exec -i excursion-gpt-postgres-prod psql -U postgres 3D_Excursion < "$backup_file"

    if [ $? -eq 0 ]; then
        print_success "Database restored successfully"

        # Restart API service
        $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE start api

        # Run migrations
        run_migrations
    else
        print_error "Failed to restore database"
        # Restart API service even if restore failed
        $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE start api
        exit 1
    fi
}

# Function to run database migrations
run_migrations() {
    local compose_cmd=$(get_compose_cmd)

    print_info "Running database migrations..."
    $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE exec api dotnet ef database update

    if [ $? -eq 0 ]; then
        print_success "Database migrations completed"
    else
        print_error "Database migrations failed"
        exit 1
    fi
}

# Function to update services
update_services() {
    local compose_cmd=$(get_compose_cmd)

    print_info "Updating production services..."

    # Backup database before update
    backup_database

    # Pull latest images and rebuild
    $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE pull
    $compose_cmd -f $COMPOSE_FILE --env-file $ENV_FILE up -d --build

    # Run migrations if needed
    run_migrations

    print_success "Services updated successfully"
}

# Function to monitor resources
monitor_resources() {
    print_info "Resource Usage:"
    echo "========================================="
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}\t{{.NetIO}}\t{{.BlockIO}}"

    echo ""
    print_info "Disk Usage:"
    echo "========================================="
    docker system df
}

# Function to clean up
cleanup() {
    print_warning "This will remove all unused Docker resources (images, containers, volumes, networks)"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Cleaning up unused Docker resources..."
        docker system prune -af
        print_success "Cleanup completed"
    else
        print_info "Cleanup cancelled"
    fi
}

# Function to show help
show_help() {
    echo "Excursion GPT Production Management Script"
    echo "=========================================="
    echo ""
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  start              Start production environment"
    echo "  stop               Stop production environment"
    echo "  restart            Restart production environment"
    echo "  status             Show service status and health checks"
    echo "  logs [service]     View logs (all services or specific service)"
    echo "  backup             Create database backup"
    echo "  restore <file>     Restore database from backup"
    echo "  migrate            Run database migrations"
    echo "  update             Update all services"
    echo "  monitor            Monitor resource usage"
    echo "  cleanup            Clean up unused Docker resources"
    echo "  help               Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 start           # Start production environment"
    echo "  $0 logs api        # View API logs"
    echo "  $0 backup          # Create database backup"
    echo "  $0 restore backups/backup_20240101_120000.sql"
    echo "  $0 update          # Update all services"
    echo ""
    echo "Services: api, postgres, minio, nginx, redis, prometheus, grafana"
    echo ""
    echo "Configuration:"
    echo "  Compose file: $COMPOSE_FILE"
    echo "  Environment:  $ENV_FILE"
    echo "  Project name: $PROJECT_NAME"
}

# Main script execution
main() {
    local command="${1:-help}"

    case "$command" in
        start)
            start_production
            ;;
        stop)
            stop_production
            ;;
        restart)
            restart_production
            ;;
        status)
            show_status
            ;;
        logs)
            view_logs "$2"
            ;;
        backup)
            backup_database
            ;;
        restore)
            restore_database "$2"
            ;;
        migrate)
            run_migrations
            ;;
        update)
            update_services
            ;;
        monitor)
            monitor_resources
            ;;
        cleanup)
            cleanup
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
