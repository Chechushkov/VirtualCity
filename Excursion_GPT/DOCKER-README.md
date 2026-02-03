# Excursion GPT - Docker Compose Deployment

This guide provides comprehensive instructions for deploying and running the Excursion GPT solution using Docker Compose.

## 🚀 Quick Start

### Prerequisites
- Docker Engine 20.10+
- Docker Compose 2.0+
- Git (optional)

### 1. Clone and Navigate
```bash
# Navigate to the project directory
cd "C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT"
```

### 2. Make Script Executable (Linux/Mac)
```bash
chmod +x docker-compose.sh
```

### 3. Start All Services
```bash
# Using the management script
./docker-compose.sh start

# Or using Docker Compose directly
docker-compose up -d
```

### 4. Verify Deployment
```bash
# Check service status
./docker-compose.sh status

# View logs
./docker-compose.sh logs
```

## 📊 Service Overview

The Docker Compose setup includes the following services:

| Service | Port | Description | Health Check |
|---------|------|-------------|--------------|
| **API** | 5000 | ASP.NET Core 9.0 Web API | `http://localhost:5000/health` |
| **PostgreSQL** | 5432 | Database for 3D excursions | Automatic via pg_isready |
| **MinIO** | 9000/9001 | Object storage for 3D models | MinIO health endpoint |
| **Nginx** | 80 | Reverse proxy (optional) | Nginx status |
| **pgAdmin** | 5050 | Database management UI (optional) | pgAdmin health |

## 🛠️ Management Commands

### Using the Management Script
```bash
# Start all services
./docker-compose.sh start

# Start specific services
./docker-compose.sh start postgres minio

# Stop all services
./docker-compose.sh stop

# Restart services
./docker-compose.sh restart api

# View service status
./docker-compose.sh status

# View logs
./docker-compose.sh logs
./docker-compose.sh logs api postgres

# Rebuild and restart
./docker-compose.sh rebuild

# Run database migrations
./docker-compose.sh migrate

# Create database backup
./docker-compose.sh backup

# Restore from backup
./docker-compose.sh restore backups/backup_20240101_120000.sql

# Clean everything (containers, volumes, networks)
./docker-compose.sh clean
```

### Using Docker Compose Directly
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f

# Rebuild specific service
docker-compose build api

# Check service status
docker-compose ps

# Execute commands in containers
docker-compose exec api dotnet --info
docker-compose exec postgres psql -U postgres -d 3D_Excursion
```

## 🔧 Configuration

### Environment Variables
Configuration is managed through the `env.config` file. Key settings include:

```bash
# Database
POSTGRES_DB=3D_Excursion
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

# MinIO
MINIO_ROOT_USER=admin
MINIO_ROOT_PASSWORD=admin12345

# Application
ASPNETCORE_ENVIRONMENT=Development
JWT__KEY=YourJwtSecretKey
```

### Customizing Configuration
1. Edit `env.config` for environment-specific settings
2. Modify `docker-compose.yml` for service configuration
3. Update `nginx/conf.d/excursion-gpt.conf` for proxy settings

## 📁 Project Structure

```
Excursion_GPT/
├── docker-compose.yml          # Main Docker Compose configuration
├── docker-compose.sh           # Management script
├── env.config                  # Environment variables
├── Dockerfile                  # ASP.NET Core application Dockerfile
├── nginx/                      # Nginx configuration
│   ├── nginx.conf
│   └── conf.d/
│       └── excursion-gpt.conf
├── backups/                    # Database backups (auto-created)
├── logs/                       # Application logs
└── init-db/                    # Database initialization scripts (optional)
```

## 🌐 Access Points

After starting the services, access them at:

- **API & Swagger UI**: http://localhost:5000
- **Swagger Documentation**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **MinIO Console**: http://localhost:9001
- **pgAdmin**: http://localhost:5050
- **PostgreSQL**: localhost:5432

### Default Credentials
- **MinIO Console**:
  - Access Key: `admin`
  - Secret Key: `admin12345`
- **pgAdmin**:
  - Email: `admin@excursion.com`
  - Password: `admin123`
- **PostgreSQL**:
  - Database: `3D_Excursion`
  - Username: `postgres`
  - Password: `postgres`

## 🔄 Database Management

### Initial Setup
The database is automatically initialized when services start. The API applies migrations on startup.

### Manual Migrations
```bash
# Run migrations manually
./docker-compose.sh migrate

# Or directly
docker-compose exec api dotnet ef database update
```

### Backup and Restore
```bash
# Create backup
./docker-compose.sh backup
# Creates: backups/backup_YYYYMMDD_HHMMSS.sql

# Restore from backup
./docker-compose.sh restore backups/backup_20240101_120000.sql
```

### Access Database
```bash
# Connect via psql
docker-compose exec postgres psql -U postgres -d 3D_Excursion

# Or use pgAdmin at http://localhost:5050
```

## 🗄️ File Storage with MinIO

### Bucket Configuration
- **Bucket Name**: `models`
- **Access**: Public read (for uploaded models)
- **Location**: `/data` inside MinIO container

### Managing Files
1. Access MinIO Console at http://localhost:9001
2. Login with credentials above
3. Navigate to "models" bucket
4. Upload/download 3D model files

### API Integration
The API automatically:
- Creates the bucket if it doesn't exist
- Configures public read access
- Provides endpoints for file upload/download

## 🚨 Troubleshooting

### Common Issues

#### 1. Port Conflicts
```bash
# Check what's using the ports
netstat -ano | findstr :5000
netstat -ano | findstr :5432

# Change ports in docker-compose.yml if needed
```

#### 2. Service Won't Start
```bash
# Check logs
./docker-compose.sh logs

# Check Docker status
docker ps -a
docker-compose ps

# Restart Docker service if needed
```

#### 3. Database Connection Issues
```bash
# Check PostgreSQL logs
./docker-compose.sh logs postgres

# Test database connection
docker-compose exec postgres pg_isready -U postgres
```

#### 4. MinIO Issues
```bash
# Check MinIO health
curl http://localhost:9000/minio/health/live

# Check bucket creation
docker-compose logs minio-create-buckets
```

### Log Files
- Application logs: `./logs/` directory
- Container logs: `docker-compose logs [service]`
- Nginx logs: Inside nginx container at `/var/log/nginx/`

## 🔒 Security Considerations

### For Production
1. **Change all default passwords** in `env.config`
2. **Use strong JWT secret** (minimum 64 characters)
3. **Enable SSL** in Nginx configuration
4. **Restrict network access** to services
5. **Regularly update** Docker images

### Security Checklist
- [ ] Change PostgreSQL password
- [ ] Change MinIO credentials
- [ ] Change pgAdmin credentials
- [ ] Use strong JWT secret
- [ ] Enable firewall rules
- [ ] Regular security updates
- [ ] Monitor access logs

## 📈 Monitoring

### Health Checks
All services include health checks. Monitor with:
```bash
# API health
curl http://localhost:5000/health

# Service status
./docker-compose.sh status
```

### Resource Usage
```bash
# View container resource usage
docker stats

# View disk usage
docker system df
```

### Log Monitoring
```bash
# Tail all logs
./docker-compose.sh logs

# Follow specific service
docker-compose logs -f api
```

## 🧹 Maintenance

### Regular Tasks
1. **Backup database**: Weekly or before updates
2. **Clean old logs**: Rotate log files
3. **Update images**: Check for security updates
4. **Monitor disk usage**: Especially MinIO storage

### Update Process
```bash
# 1. Backup database
./docker-compose.sh backup

# 2. Pull latest code
git pull origin main

# 3. Rebuild and restart
./docker-compose.sh rebuild

# 4. Run migrations if needed
./docker-compose.sh migrate

# 5. Verify deployment
./docker-compose.sh status
```

### Cleanup
```bash
# Remove unused Docker resources
docker system prune -a

# Remove specific volumes
docker volume ls
docker volume rm [volume_name]
```

## 🆘 Support

### Getting Help
1. Check logs: `./docker-compose.sh logs`
2. Verify configuration: `env.config` and `docker-compose.yml`
3. Test individual services
4. Check Docker status: `docker info`

### Debug Commands
```bash
# Shell into containers
docker-compose exec api bash
docker-compose exec postgres bash

# View container details
docker inspect [container_name]

# Check network connectivity
docker-compose exec api ping postgres
```

### Common Solutions
- **Service not starting**: Check port conflicts and resource limits
- **Database errors**: Verify credentials and connection string
- **File upload issues**: Check MinIO health and bucket permissions
- **API errors**: Check application logs and environment variables

## 📝 Notes

### Development vs Production
- **Development**: Uses default credentials, HTTP, no SSL
- **Production**: Change all passwords, enable SSL, use secure configurations

### Performance Tips
1. Adjust resource limits in `docker-compose.yml`
2. Use volume mounts for persistent data
3. Configure Nginx caching for static files
4. Monitor and adjust database connection pool

### Customization
- Modify `docker-compose.yml` to add/remove services
- Extend `Dockerfile` for additional dependencies
- Add custom Nginx configurations
- Implement custom health checks

---

## 🎯 Success Indicators

Your Docker Compose deployment is successful when:

1. ✅ All services show "Up" status
2. ✅ API health endpoint returns `{"status":"Healthy"}`
3. ✅ Swagger UI is accessible
4. ✅ Database migrations complete without errors
5. ✅ MinIO bucket is created and accessible
6. ✅ File upload/download works through API

---

**Last Updated**: $(date +%Y-%m-%d)

For issues or questions, check the logs first and refer to this documentation.