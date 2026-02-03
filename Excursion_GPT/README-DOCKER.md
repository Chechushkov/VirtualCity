# Excursion GPT - Docker Compose Deployment Guide

## 🚀 Overview

This project provides a complete Docker Compose setup for the Excursion GPT solution, a .NET 9.0 web API for managing 3D excursions and building models. The setup includes multiple configuration options for different use cases.

## 📋 Prerequisites

### For All Environments
- Docker Engine 20.10+
- Docker Compose 2.0+

### For Windows Users
- Windows 10/11 Pro, Enterprise, or Education
- WSL 2 (Windows Subsystem for Linux 2)
- Docker Desktop for Windows

### For Linux/Mac Users
- Docker Engine
- Docker Compose

## 📁 Project Structure

```
Excursion_GPT/
├── docker-compose.yml              # Full configuration (all services)
├── docker-compose.dev.yml          # Development configuration
├── docker-compose.simple.yml       # Simple config (API + DB + MinIO only)
├── docker-compose.prod.yml         # Production configuration
├── Dockerfile                      # ASP.NET Core application
├── docker-compose.sh               # Bash management script
├── start-dev.sh                    # Development quick start (Bash)
├── start-simple.sh                 # Simple version startup (Bash)
├── start-dev.ps1                   # Development quick start (PowerShell)
├── manage-prod.sh                  # Production management script
├── env.config                      # Development environment variables
├── env.production.example          # Production environment template
├── README-DOCKER.md               # This file
├── WINDOWS-README.md              # Windows-specific instructions
├── nginx/                         # Nginx configuration (optional)
│   ├── nginx.conf
│   └── conf.d/
│       └── excursion-gpt.conf
├── logs/                          # Application logs directory
└── backups/                       # Database backups (auto-created)
```

## 🏃‍♂️ Quick Start - Choose Your Option

### Option 1: Simple Version (Recommended for Development)
**No Nginx, just API + PostgreSQL + MinIO**

```bash
# Navigate to project directory
cd "C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT"

# Make script executable (Linux/Mac/WSL)
chmod +x start-simple.sh

# Start services
./start-simple.sh

# Or using Docker Compose directly
docker-compose -f docker-compose.simple.yml up -d
```

**Access:**
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- MinIO Console: http://localhost:9001
- PostgreSQL: localhost:5432

### Option 2: Development Version (With Nginx)
**Includes Nginx reverse proxy**

```bash
# Using the management script
./start-dev.sh

# Or using Docker Compose directly
docker-compose -f docker-compose.dev.yml up -d
```

### Option 3: Full Version (All Services)
**Includes Nginx, pgAdmin, and all optional services**

```bash
# Using the management script
./docker-compose.sh start

# Or using Docker Compose directly
docker-compose up -d
```

### Option 4: Production Version
**With monitoring, SSL, and security features**

```bash
# 1. Copy environment template
cp env.production.example .env.production

# 2. Edit .env.production and set all values
#    IMPORTANT: Change all passwords and secrets!

# 3. Start production environment
./manage-prod.sh start
```

## 🎯 Which Option Should You Choose?

### For Local Development
✅ **Use Simple Version (`docker-compose.simple.yml`)**
- No unnecessary complexity
- Direct access to API on port 5000
- Faster startup
- Easier debugging

### When You Need Nginx
✅ **Use Development Version (`docker-compose.dev.yml`)**
- Need SSL/TLS termination
- Have multiple services to route
- Need rate limiting
- Serving static files

### For Production Deployment
✅ **Use Production Version (`docker-compose.prod.yml`)**
- Need monitoring (Prometheus/Grafana)
- Require Redis caching
- Need automatic updates (Watchtower)
- Require SSL and security features

## 🛠️ Available Services by Configuration

### Simple Version (`docker-compose.simple.yml`)
| Service | Port | Description |
|---------|------|-------------|
| **API** | 5000 | ASP.NET Core 9.0 Web API |
| **PostgreSQL** | 5432 | Database for 3D excursions |
| **MinIO** | 9000/9001 | Object storage for 3D models |

### Development Version (`docker-compose.dev.yml`)
| Service | Port | Description |
|---------|------|-------------|
| **API** | 5000 | ASP.NET Core 9.0 Web API |
| **PostgreSQL** | 5432 | Database |
| **MinIO** | 9000/9001 | Object storage |
| **Nginx** | 80 | Reverse proxy |

### Full Version (`docker-compose.yml`)
| Service | Port | Description |
|---------|------|-------------|
| **API** | 5000 | ASP.NET Core 9.0 Web API |
| **PostgreSQL** | 5432 | Database |
| **MinIO** | 9000/9001 | Object storage |
| **Nginx** | 80 | Reverse proxy |
| **pgAdmin** | 5050 | Database management UI |

### Production Version (`docker-compose.prod.yml`)
| Service | Port | Description |
|---------|------|-------------|
| **API** | 5000 | ASP.NET Core 9.0 Web API |
| **PostgreSQL** | 5432 | Database with persistence |
| **MinIO** | 9000/9001 | Object storage with persistence |
| **Nginx** | 80/443 | Reverse proxy with SSL |
| **Redis** | 6379 | Caching layer |
| **Prometheus** | 9090 | Monitoring |
| **Grafana** | 3000 | Dashboards |
| **Watchtower** | - | Automatic updates |

## 🔧 Management Commands

### Simple Version Management
```bash
# Start services
./start-simple.sh

# Stop services
./start-simple.sh stop

# View logs
./start-simple.sh logs

# Show status
./start-simple.sh status

# Run migrations
./start-simple.sh migrate
```

### Development Management
```bash
# Start all services
./docker-compose.sh start

# Start specific services
./docker-compose.sh start postgres minio

# View service status
./docker-compose.sh status

# View logs
./docker-compose.sh logs

# Run database migrations
./docker-compose.sh migrate

# Create database backup
./docker-compose.sh backup
```

### Production Management
```bash
# Start production environment
./manage-prod.sh start

# Show status and health checks
./manage-prod.sh status

# View logs
./manage-prod.sh logs api

# Create backup
./manage-prod.sh backup

# Update all services
./manage-prod.sh update
```

### PowerShell Script (Windows)
```powershell
# Start development environment
.\start-dev.ps1

# Stop services
.\start-dev.ps1 stop

# View logs
.\start-dev.ps1 logs

# Show status
.\start-dev.ps1 status
```

## 🌐 Access Points

### Simple Version (Recommended)
- **API & Swagger UI**: http://localhost:5000
- **Health Check**: http://localhost:5000/health
- **MinIO Console**: http://localhost:9001
- **PostgreSQL**: localhost:5432

### With Nginx (Development/Full Version)
- **API**: http://localhost:5000 (via Nginx)
- **Swagger UI**: http://localhost:5000/swagger
- **MinIO Console**: http://localhost:9001
- **pgAdmin** (if enabled): http://localhost:5050

### Production Version
- **API**: http://localhost:5000
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000
- **MinIO Console**: http://localhost:9001

### Default Credentials
- **MinIO Console**:
  - Access Key: `admin`
  - Secret Key: `admin12345`
- **PostgreSQL**:
  - Database: `3D_Excursion`
  - Username: `postgres`
  - Password: `postgres`
- **pgAdmin** (if enabled):
  - Email: `admin@excursion.com`
  - Password: `admin123`

## 🔒 Security Configuration

### For Production Deployment

1. **Change all default passwords** in `.env.production`:
   ```bash
   # Generate secure passwords
   openssl rand -base64 64    # For JWT_KEY
   openssl rand -base64 32    # For database and MinIO passwords
   ```

2. **Configure SSL certificates** (if using Nginx):
   ```bash
   mkdir -p nginx/ssl
   # Place your certificate.crt and private.key here
   ```

3. **Set proper file permissions**:
   ```bash
   chmod 600 .env.production
   chmod 700 nginx/ssl
   ```

### Security Checklist
- [ ] All passwords changed from defaults
- [ ] JWT_KEY is 64+ characters
- [ ] SSL certificates configured (if using Nginx)
- [ ] Regular backups enabled
- [ ] Monitoring enabled (production)
- [ ] Rate limiting configured (if using Nginx)

## 📊 Database Management

### Initial Setup
The database is automatically initialized when services start. The API applies migrations on startup.

### Manual Operations
```bash
# Run migrations
./start-simple.sh migrate

# Create backup
./docker-compose.sh backup
# Creates: backups/backup_YYYYMMDD_HHMMSS.sql

# Restore from backup
./docker-compose.sh restore backups/backup_20240101_120000.sql

# Access database directly
docker-compose exec postgres psql -U postgres -d 3D_Excursion
```

## 🗄️ File Storage with MinIO

### Bucket Configuration
- **Bucket Name**: `models`
- **Access**: Public read (for uploaded models)
- **Location**: `/data` inside MinIO container

### Managing Files
1. Access MinIO Console at http://localhost:9001
2. Login with credentials
3. Navigate to "models" bucket
4. Upload/download 3D model files

### API Integration
The API automatically:
- Creates the bucket if it doesn't exist
- Configures public read access
- Provides endpoints for file upload/download

## 🚨 Troubleshooting

### Common Issues

#### Port Conflicts
```bash
# Check what's using the ports
netstat -ano | findstr :5000  # Windows
lsof -i :5000                 # Linux/Mac

# Change ports in docker-compose.yml if needed
```

#### Service Won't Start
```bash
# Check logs
docker-compose logs

# Check Docker status
docker ps -a
docker-compose ps

# Restart Docker service if needed
```

#### Database Connection Issues
```bash
# Check PostgreSQL logs
docker-compose logs postgres

# Test database connection
docker-compose exec postgres pg_isready -U postgres
```

#### Windows-Specific Issues
1. **WSL 2 not installed**: Run `wsl --install`
2. **Docker Desktop won't start**: Enable virtualization in BIOS
3. **Slow performance**: Increase Docker resources in Settings
4. **Permission errors**: Reset Docker to factory defaults

### Log Files
- Application logs: `./logs/` directory
- Container logs: `docker-compose logs [service]`
- Nginx logs: Inside nginx container at `/var/log/nginx/` (if using Nginx)

## 📈 Monitoring & Maintenance

### Health Checks
All services include health checks. Monitor with:
```bash
# API health
curl http://localhost:5000/health

# Service status
./start-simple.sh status
```

### Resource Monitoring
```bash
# View container resource usage
docker stats

# View disk usage
docker system df

# Production monitoring
./manage-prod.sh monitor
```

### Regular Maintenance Tasks
1. **Weekly**: Database backups
2. **Monthly**: Log rotation and cleanup
3. **Quarterly**: Security updates
4. **As needed**: Docker image updates

### Update Process
```bash
# Simple version
docker-compose -f docker-compose.simple.yml up -d --build

# Production
./manage-prod.sh update
```

## 🔄 Deployment Workflows

### Development Workflow (Simple Version)
```bash
# 1. Start environment
./start-simple.sh

# 2. Make code changes
# 3. Test changes
curl http://localhost:5000/health

# 4. View logs if needed
./start-simple.sh logs

# 5. Stop when done
./start-simple.sh stop
```

### Production Deployment
```bash
# 1. Prepare environment
cp env.production.example .env.production
# Edit .env.production with production values

# 2. Start production
./manage-prod.sh start

# 3. Verify deployment
./manage-prod.sh status

# 4. Monitor
./manage-prod.sh logs api

# 5. Update when needed
./manage-prod.sh update
```

## 🆘 Getting Help

### Debug Commands
```bash
# Shell into containers
docker-compose exec api bash
docker-compose exec postgres bash

# View container details
docker inspect [container_name]

# Check network connectivity
docker-compose exec api ping postgres

# View Docker system info
docker info
docker system info
```

### Common Solutions
- **Service not starting**: Check port conflicts and resource limits
- **Database errors**: Verify credentials and connection string
- **File upload issues**: Check MinIO health and bucket permissions
- **API errors**: Check application logs and environment variables

### Support Resources
1. Check logs: `docker-compose logs`
2. Verify configuration: Environment files and compose files
3. Test individual services
4. Check Docker status: `docker info`
5. Consult documentation in this README

## 📝 Notes

### When to Use Nginx
✅ **Use Nginx if you need:**
- SSL/TLS termination
- Multiple services routing
- Rate limiting
- Static file serving
- Load balancing

❌ **Skip Nginx if:**
- You only have an API
- You're in development
- You don't need SSL
- You want simpler setup

### Performance Tips
1. Adjust resource limits in compose files
2. Use volume mounts for persistent data
3. Configure Nginx caching for static files (if using Nginx)
4. Monitor and adjust database connection pool
5. Use Redis for caching in production

### Customization
- Modify `docker-compose.yml` to add/remove services
- Extend `Dockerfile` for additional dependencies
- Add custom Nginx configurations (if using Nginx)
- Implement custom health checks
- Add monitoring and logging services

## 🎯 Success Indicators

Your Docker Compose deployment is successful when:

1. ✅ All services show "Up" status
2. ✅ API health endpoint returns `{"status":"Healthy"}`
3. ✅ Swagger UI is accessible (development)
4. ✅ Database migrations complete without errors
5. ✅ MinIO bucket is created and accessible
6. ✅ File upload/download works through API
7. ✅ Monitoring shows healthy services (production)

## 📞 Support

For issues or questions:
1. Check the logs first: `docker-compose logs`
2. Verify configuration files
3. Test individual services
4. Consult this documentation
5. Check Docker and Docker Compose documentation

---

**Last Updated**: 2024-01-01

**Project Location**: `C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT`

**Recommended for most users**: Use the **Simple Version** (`./start-simple.sh`) for development and testing.

**Important**: Always backup your data before making significant changes to the deployment configuration.