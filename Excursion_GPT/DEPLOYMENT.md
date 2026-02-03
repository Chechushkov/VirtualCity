# Excursion_GPT Deployment Guide

This guide provides comprehensive instructions for deploying the Excursion_GPT solution to a Debian server via SSH.

## 🎯 Overview

Excursion_GPT is a .NET 9.0 web API for managing 3D excursions and building models. This deployment guide covers server setup, database configuration, application deployment, and production environment setup.

## 📋 Prerequisites

- Debian 11/12 server with SSH access
- Root or sudo access on the server
- Domain name (optional, for SSL)
- Local development environment with .NET 9.0 SDK

## 🚀 Quick Deployment Script

For automated deployment, use the provided script:

```bash
# Make the script executable
chmod +x deploy.sh

# Run the deployment script
./deploy.sh
```

## 🔧 Manual Deployment Steps

### Step 1: Server Preparation

Connect to your server and install required dependencies:

```bash
# Connect to server
ssh username@your-server-ip

# Update system
sudo apt update && sudo apt upgrade -y

# Install dependencies
sudo apt install -y curl wget gnupg software-properties-common
```

### Step 2: Install .NET 9.0 Runtime

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET runtime
sudo apt update
sudo apt install -y dotnet-runtime-9.0 aspnetcore-runtime-9.0

# Verify installation
dotnet --info
```

### Step 3: Database Setup (PostgreSQL)

```bash
# Install PostgreSQL
sudo apt install -y postgresql postgresql-contrib

# Start and enable PostgreSQL
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Configure database
sudo -u postgres psql -c "CREATE DATABASE \"3D_Excursion\";"
sudo -u postgres psql -c "CREATE USER excursion_user WITH PASSWORD 'your_secure_password_here';"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE \"3D_Excursion\" TO excursion_user;"

# Update PostgreSQL authentication (optional)
sudo nano /etc/postgresql/*/main/pg_hba.conf
# Add: host 3D_Excursion excursion_user 127.0.0.1/32 md5

# Restart PostgreSQL
sudo systemctl restart postgresql
```

### Step 4: Install and Configure Nginx

```bash
# Install Nginx
sudo apt install -y nginx

# Create Nginx configuration
sudo nano /etc/nginx/sites-available/excursion-gpt
```

Add the following configuration:

```nginx
server {
    listen 80;
    server_name your-domain.com; # Replace with your domain or IP

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        
        # Increase timeout for file uploads
        proxy_connect_timeout 600;
        proxy_send_timeout 600;
        proxy_read_timeout 600;
    }

    # Optional: Serve static files directly
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/excursion-gpt /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl enable nginx
sudo systemctl start nginx
```

### Step 5: Application Deployment

#### Option A: Manual File Transfer

```bash
# On your local machine, build the application
cd "C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT"
dotnet publish -c Release -o ./publish

# Create deployment directory on server
ssh username@your-server-ip "sudo mkdir -p /var/www/excursion-gpt"
ssh username@your-server-ip "sudo chown \$USER:\$USER /var/www/excursion-gpt"

# Transfer files using SCP
scp -r ./publish/* username@your-server-ip:/var/www/excursion-gpt/

# Or using rsync (more efficient)
rsync -avz ./publish/ username@your-server-ip:/var/www/excursion-gpt/
```

#### Option B: Git-based Deployment

```bash
# On server, clone repository
sudo mkdir -p /var/www/excursion-gpt
sudo chown $USER:$USER /var/www/excursion-gpt
cd /var/www/excursion-gpt
git clone https://github.com/your-username/Excursion_GPT.git .
dotnet publish -c Release -o .
```

### Step 6: Environment Configuration

Create production configuration:

```bash
sudo nano /var/www/excursion-gpt/appsettings.Production.json
```

```json
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
    "DefaultConnection": "Host=localhost;Port=5432;Database=3D_Excursion;Username=excursion_user;Password=your_secure_password_here"
  },
  "Jwt": {
    "Key": "YourVeryLongAndSecureJWTSecretKeyMinimum64CharactersLongForSecurity",
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
```

### Step 7: Systemd Service Configuration

Create a systemd service file:

```bash
sudo nano /etc/systemd/system/excursion-gpt.service
```

```ini
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
```

Set permissions and enable the service:

```bash
sudo chown -R www-data:www-data /var/www/excursion-gpt
sudo systemctl daemon-reload
sudo systemctl enable excursion-gpt.service
sudo systemctl start excursion-gpt.service
```

### Step 8: Database Migration

```bash
# Apply database migrations
cd /var/www/excursion-gpt
sudo -u www-data dotnet ef database update

# Or if EF tools are not installed:
sudo apt install -y dotnet-sdk-9.0
dotnet tool install --global dotnet-ef
sudo -u www-data dotnet ef database update
```

### Step 9: SSL Certificate (Recommended)

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obtain SSL certificate
sudo certbot --nginx -d your-domain.com

# Auto-renewal setup
sudo crontab -e
# Add: 0 12 * * * /usr/bin/certbot renew --quiet
```

### Step 10: Firewall Configuration

```bash
# Enable firewall
sudo ufw enable

# Allow necessary ports
sudo ufw allow ssh
sudo ufw allow 80
sudo ufw allow 443
sudo ufw allow 5432  # PostgreSQL (if needed externally)
```

### Step 11: MinIO Setup (Optional)

If using MinIO for file storage:

```bash
# Download MinIO
wget https://dl.min.io/server/minio/release/linux-amd64/minio
chmod +x minio
sudo mv minio /usr/local/bin/

# Create MinIO user and directories
sudo useradd -r minio-user -s /bin/false
sudo mkdir -p /opt/minio/data
sudo chown minio-user:minio-user /opt/minio/data

# Create MinIO service
sudo nano /etc/systemd/system/minio.service
```

## 🛠️ Troubleshooting

### Common Issues and Solutions

#### Application Won't Start
```bash
# Check service status
sudo systemctl status excursion-gpt.service

# View logs
sudo journalctl -u excursion-gpt.service -f

# Check application logs
sudo tail -f /var/www/excursion-gpt/logs/app.log
```

#### Database Connection Issues
```bash
# Test database connection
sudo -u postgres psql -d "3D_Excursion" -U excursion_user

# Check PostgreSQL status
sudo systemctl status postgresql

# Verify connection string
cat /var/www/excursion-gpt/appsettings.Production.json | grep ConnectionString
```

#### Nginx Issues
```bash
# Test Nginx configuration
sudo nginx -t

# Check Nginx logs
sudo tail -f /var/log/nginx/error.log

# Restart Nginx
sudo systemctl restart nginx
```

#### Permission Issues
```bash
# Fix permissions
sudo chown -R www-data:www-data /var/www/excursion-gpt
sudo chmod -R 755 /var/www/excursion-gpt
sudo find /var/www/excursion-gpt -type f -exec chmod 644 {} \;
```

### Health Check Endpoints

- API Health: `http://your-domain.com/health`
- Swagger UI: `http://your-domain.com/swagger`
- Database Check: Check logs for migration success

## 🔄 Update Process

When deploying updates:

```bash
# Stop the service
sudo systemctl stop excursion-gpt.service

# Backup current version
sudo cp -r /var/www/excursion-gpt /var/www/excursion-gpt-backup-$(date +%Y%m%d)

# Deploy new version (repeat Step 5)

# Apply database migrations
cd /var/www/excursion-gpt
sudo -u www-data dotnet ef database update

# Restart service
sudo systemctl start excursion-gpt.service

# Verify deployment
curl http://localhost:5000/health
```

## 📊 Monitoring and Maintenance

### Log Rotation
```bash
sudo nano /etc/logrotate.d/excursion-gpt

# Add:
/var/www/excursion-gpt/logs/*.log {
    daily
    missingok
    rotate 14
    compress
    delaycompress
    notifempty
    copytruncate
}
```

### Performance Monitoring
```bash
# Install monitoring tools
sudo apt install -y htop iotop nethogs

# Monitor application
htop
sudo journalctl -u excursion-gpt.service --since "1 hour ago"
```

## 🔒 Security Checklist

- [ ] Change default PostgreSQL password
- [ ] Use strong JWT secret key
- [ ] Enable firewall
- [ ] Configure SSL/TLS
- [ ] Set proper file permissions
- [ ] Regular security updates
- [ ] Monitor logs for suspicious activity
- [ ] Backup database regularly

## 📞 Support

If you encounter issues:

1. Check application logs: `sudo journalctl -u excursion-gpt.service -f`
2. Verify database connectivity
3. Check Nginx configuration
4. Review file permissions
5. Consult this deployment guide

## 🎉 Success Indicators

Your deployment is successful when:

- ✅ Service status: `active (running)`
- ✅ Health endpoint returns: `{"status":"Healthy","timestamp":"..."}`
- ✅ Database migrations applied without errors
- ✅ Nginx serves the application correctly
- ✅ SSL certificate valid (if configured)
- ✅ API endpoints accessible via Swagger UI

---

**Note**: Replace all placeholder values (passwords, domains, IPs) with your actual configuration before deployment.

Last updated: $(date)