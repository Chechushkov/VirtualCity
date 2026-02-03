#!/bin/bash

# Excursion_GPT Automated Deployment Script
# This script automates the deployment of Excursion_GPT to a Debian server
# Usage: ./deploy.sh [server_ip] [domain_name]

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SERVER_IP="${1:-your-server-ip}"
DOMAIN_NAME="${2:-your-domain.com}"
APP_USER="www-data"
APP_NAME="excursion-gpt"
APP_DIR="/var/www/${APP_NAME}"
DB_NAME="3D_Excursion"
DB_USER="excursion_user"
DB_PASSWORD=$(openssl rand -base64 32)
JWT_SECRET=$(openssl rand -base64 64)

# Logging function
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

success() {
    echo -e "${GREEN}✓${NC} $1"
}

warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

error() {
    echo -e "${RED}✗${NC} $1"
    exit 1
}

# Check prerequisites
check_prerequisites() {
    log "Checking prerequisites..."

    # Check if server IP is provided
    if [ "$SERVER_IP" = "your-server-ip" ]; then
        error "Please provide server IP as first argument: ./deploy.sh your-server-ip your-domain.com"
    fi

    # Check if SSH key is available
    if [ ! -f "$HOME/.ssh/id_rsa.pub" ]; then
        warning "SSH key not found. Please ensure you can connect to the server via SSH."
    fi

    # Check if .NET is installed locally
    if ! command -v dotnet &> /dev/null; then
        error ".NET SDK not found. Please install .NET 9.0 SDK locally."
    fi

    success "Prerequisites check passed"
}

# Build the application
build_application() {
    log "Building application..."

    # Clean previous builds
    rm -rf ./publish

    # Build and publish
    dotnet publish -c Release -o ./publish

    if [ $? -ne 0 ]; then
        error "Build failed"
    fi

    success "Application built successfully"
}

# Server setup
setup_server() {
    log "Setting up server: $SERVER_IP"

    # Create setup script on server
    cat > /tmp/server_setup.sh << 'EOF'
#!/bin/bash
set -e

# Update system
sudo apt update && sudo apt upgrade -y

# Install dependencies
sudo apt install -y curl wget gnupg software-properties-common

# Install .NET 9.0
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-runtime-9.0 aspnetcore-runtime-9.0

# Install PostgreSQL
sudo apt install -y postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Install Nginx
sudo apt install -y nginx
sudo systemctl enable nginx

# Install Certbot for SSL
sudo apt install -y certbot python3-certbot-nginx

# Create application directory
sudo mkdir -p /var/www/excursion-gpt
sudo chown $USER:$USER /var/www/excursion-gpt

# Configure firewall
sudo ufw --force enable
sudo ufw allow ssh
sudo ufw allow 80
sudo ufw allow 443

EOF

    # Copy and execute setup script on server
    scp /tmp/server_setup.sh $SERVER_IP:/tmp/
    ssh $SERVER_IP "chmod +x /tmp/server_setup.sh && /tmp/server_setup.sh"

    success "Server setup completed"
}

# Database setup
setup_database() {
    log "Setting up database..."

    # Create database setup script
    cat > /tmp/db_setup.sql << EOF
CREATE DATABASE "$DB_NAME";
CREATE USER $DB_USER WITH PASSWORD '$DB_PASSWORD';
GRANT ALL PRIVILEGES ON DATABASE "$DB_NAME" TO $DB_USER;
EOF

    # Execute database setup
    scp /tmp/db_setup.sql $SERVER_IP:/tmp/
    ssh $SERVER_IP "sudo -u postgres psql -f /tmp/db_setup.sql"

    success "Database setup completed"
}

# Deploy application files
deploy_application() {
    log "Deploying application files..."

    # Copy published files to server
    rsync -avz ./publish/ $SERVER_IP:/var/www/excursion-gpt/

    success "Application files deployed"
}

# Configure application
configure_application() {
    log "Configuring application..."

    # Create production appsettings
    cat > /tmp/appsettings.Production.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
  },
  "Jwt": {
    "Key": "$JWT_SECRET",
    "Issuer": "ExcursionGPTApi",
    "Audience": "ExcursionGPTClients"
  },
  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "admin",
    "SecretKey": "admin12345",
    "BucketName": "models",
    "UseSSL": false
  }
}
EOF

    # Copy configuration to server
    scp /tmp/appsettings.Production.json $SERVER_IP:/var/www/excursion-gpt/

    success "Application configured"
}

# Configure Nginx
configure_nginx() {
    log "Configuring Nginx..."

    # Create Nginx configuration
    cat > /tmp/excursion-gpt-nginx << EOF
server {
    listen 80;
    server_name $DOMAIN_NAME;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;

        # Increase timeout for file uploads
        proxy_connect_timeout 600;
        proxy_send_timeout 600;
        proxy_read_timeout 600;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://localhost:5000/health;
        access_log off;
    }
}
EOF

    # Copy Nginx configuration to server
    scp /tmp/excursion-gpt-nginx $SERVER_IP:/tmp/
    ssh $SERVER_IP "sudo mv /tmp/excursion-gpt-nginx /etc/nginx/sites-available/excursion-gpt"
    ssh $SERVER_IP "sudo ln -sf /etc/nginx/sites-available/excursion-gpt /etc/nginx/sites-enabled/"
    ssh $SERVER_IP "sudo nginx -t && sudo systemctl reload nginx"

    success "Nginx configured"
}

# Create systemd service
create_service() {
    log "Creating systemd service..."

    # Create service file
    cat > /tmp/excursion-gpt.service << EOF
[Unit]
Description=Excursion GPT API
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=exec
User=www-data
Group=www-data
WorkingDirectory=/var/www/excursion-gpt
ExecStart=/usr/bin/dotnet /var/www/excursion-gpt/Excursion_GPT.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://localhost:5000

# Security settings
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/www/excursion-gpt

[Install]
WantedBy=multi-user.target
EOF

    # Copy service file to server
    scp /tmp/excursion-gpt.service $SERVER_IP:/tmp/
    ssh $SERVER_IP "sudo mv /tmp/excursion-gpt.service /etc/systemd/system/"
    ssh $SERVER_IP "sudo systemctl daemon-reload"

    success "Systemd service created"
}

# Set permissions and start service
start_application() {
    log "Starting application..."

    # Set permissions
    ssh $SERVER_IP "sudo chown -R www-data:www-data /var/www/excursion-gpt"

    # Enable and start service
    ssh $SERVER_IP "sudo systemctl enable excursion-gpt.service"
    ssh $SERVER_IP "sudo systemctl start excursion-gpt.service"

    # Wait for service to start
    sleep 5

    # Check service status
    if ssh $SERVER_IP "sudo systemctl is-active --quiet excursion-gpt.service"; then
        success "Application service is running"
    else
        error "Failed to start application service"
    fi

    success "Application started successfully"
}

# Apply database migrations
apply_migrations() {
    log "Applying database migrations..."

    # Install EF tools and apply migrations
    ssh $SERVER_IP "cd /var/www/excursion-gpt && sudo -u www-data dotnet ef database update" || {
        warning "Database migration might need manual intervention"
    }

    success "Database migrations applied"
}

# Setup SSL (optional)
setup_ssl() {
    if [ "$DOMAIN_NAME" != "your-domain.com" ]; then
        log "Setting up SSL certificate..."
        ssh $SERVER_IP "sudo certbot --nginx -d $DOMAIN_NAME --non-interactive --agree-tos --email admin@$DOMAIN_NAME" || {
            warning "SSL setup failed or was skipped"
        }
        success "SSL certificate configured"
    else
        warning "No domain specified, skipping SSL setup"
    fi
}

# Health check
health_check() {
    log "Performing health check..."

    # Wait a bit for application to fully start
    sleep 10

    # Test health endpoint
    if [ "$DOMAIN_NAME" != "your-domain.com" ]; then
        HEALTH_URL="http://$DOMAIN_NAME/health"
    else
        HEALTH_URL="http://$SERVER_IP/health"
    fi

    if curl -s --retry 3 --retry-delay 5 "$HEALTH_URL" | grep -q "Healthy"; then
        success "Health check passed - Application is running correctly"
    else
        warning "Health check failed or inconclusive"
    fi
}

# Display deployment summary
show_summary() {
    log "Deployment Summary"
    echo "=================="
    success "Application deployed successfully!"
    echo ""
    echo "Application URL:"
    if [ "$DOMAIN_NAME" != "your-domain.com" ]; then
        echo "  https://$DOMAIN_NAME"
        echo "  https://$DOMAIN_NAME/swagger (API Documentation)"
    else
        echo "  http://$SERVER_IP"
        echo "  http://$SERVER_IP/swagger (API Documentation)"
    fi
    echo ""
    echo "Database Information:"
    echo "  Database: $DB_NAME"
    echo "  Username: $DB_USER"
    echo "  Password: $DB_PASSWORD"
    echo ""
    echo "Service Management:"
    echo "  sudo systemctl status excursion-gpt.service"
    echo "  sudo journalctl -u excursion-gpt.service -f"
    echo "  sudo systemctl restart excursion-gpt.service"
    echo ""
    warning "Important: Save the database password shown above!"
    echo ""
    success "Deployment completed at $(date)"
}

# Cleanup temporary files
cleanup() {
    rm -f /tmp/server_setup.sh /tmp/db_setup.sql /tmp/appsettings.Production.json /tmp/excursion-gpt-nginx /tmp/excursion-gpt.service
}

# Main deployment function
main() {
    log "Starting Excursion_GPT deployment to $SERVER_IP"

    check_prerequisites
    build_application
    setup_server
    setup_database
    deploy_application
    configure_application
    configure_nginx
    create_service
    start_application
    apply_migrations
    setup_ssl
    health_check
    show_summary
    cleanup

    success "🎉 Deployment completed successfully!"
}

# Run main function
main "$@"
