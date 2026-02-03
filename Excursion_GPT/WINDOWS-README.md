# Excursion GPT - Windows Docker Setup Guide

This guide provides Windows-specific instructions for setting up and running the Excursion GPT solution using Docker Compose.

## 🚀 Quick Start for Windows

### Prerequisites

1. **Docker Desktop for Windows**
   - Download from: https://www.docker.com/products/docker-desktop/
   - Requires Windows 10/11 Pro, Enterprise, or Education (with WSL 2)
   - For Windows Home: Install WSL 2 first, then Docker Desktop

2. **Enable WSL 2 (Windows Subsystem for Linux)**
   ```powershell
   # Open PowerShell as Administrator and run:
   wsl --install
   # Restart your computer when prompted
   ```

3. **Git for Windows** (Optional)
   - Download from: https://git-scm.com/download/win

### Installation Steps

#### Step 1: Install Docker Desktop
1. Download Docker Desktop for Windows
2. Run the installer and follow the prompts
3. Enable WSL 2 backend when prompted
4. Restart your computer if required

#### Step 2: Verify Installation
```powershell
# Open PowerShell and check Docker version
docker --version
docker-compose --version

# Check WSL 2 is working
wsl --list --verbose
```

#### Step 3: Configure Docker Desktop
1. Open Docker Desktop from Start Menu
2. Go to Settings → Resources → WSL Integration
3. Enable integration with your default WSL distribution
4. Allocate at least 4GB RAM and 2 CPUs for optimal performance

### 🏃‍♂️ Running the Application

#### Method 1: Using PowerShell (Recommended)

```powershell
# Navigate to the project directory
cd "C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT"

# Start the development environment
.\start-dev.ps1

# Or start specific services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f api
```

#### Method 2: Using Command Prompt

```cmd
REM Navigate to the project directory
cd "C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT"

REM Start services
docker-compose up -d

REM Check if services are running
docker ps
```

#### Method 3: Using Docker Desktop Dashboard
1. Open Docker Desktop
2. Click on "Containers" in the left sidebar
3. Click "Add" button and select "Compose"
4. Navigate to `Excursion_GPT` folder and select `docker-compose.yml`
5. Click "Run" to start all services

### 📁 Project Structure for Windows

```
C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT\
├── docker-compose.yml          # Main configuration
├── docker-compose.dev.yml      # Development configuration
├── start-dev.ps1              # PowerShell startup script
├── start-dev.sh               # Bash startup script (WSL)
├── Dockerfile                 # ASP.NET Core application
├── env.config                 # Environment variables
├── nginx\                     # Nginx configuration
│   ├── nginx.conf
│   └── conf.d\
│       └── excursion-gpt.conf
└── logs\                      # Application logs
```

### 🔧 Windows-Specific Configuration

#### Port Configuration
If ports are already in use, modify `docker-compose.yml`:

```yaml
# Change these port mappings if conflicts occur
ports:
  - "5001:80"    # Instead of 5000:80
  - "5433:5432"  # Instead of 5432:5432
  - "9002:9000"  # Instead of 9000:9000
```

#### Volume Mounts on Windows
Windows path mounts need special syntax:

```yaml
volumes:
  # For WSL 2 backend
  - /mnt/c/Users/DELL G15/Documents/Experiments/GPT/Excursion_GPT/logs:/app/logs
  
  # For Windows containers (not recommended for Linux images)
  - C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT\logs:/app/logs
```

### 🚨 Common Windows Issues & Solutions

#### Issue 1: "Port already in use"
```powershell
# Find what's using the port
netstat -ano | findstr :5000

# Kill the process (replace PID with actual number)
taskkill /PID <PID> /F

# Or change the port in docker-compose.yml
```

#### Issue 2: "WSL 2 not installed"
```powershell
# Install WSL 2
wsl --install -d Ubuntu

# Set WSL 2 as default
wsl --set-default-version 2

# Update WSL
wsl --update
```

#### Issue 3: "Docker Desktop won't start"
1. Check Windows version (must be 10/11 Pro, Enterprise, or Education)
2. Enable virtualization in BIOS/UEFI
3. Enable Hyper-V and Windows Hypervisor Platform:
   ```powershell
   # Run as Administrator
   Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
   Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform
   ```
4. Restart computer

#### Issue 4: "Slow performance"
1. Increase Docker resources:
   - Docker Desktop → Settings → Resources
   - RAM: 6-8GB, CPUs: 4+, Swap: 1GB
2. Exclude project folders from Windows Defender:
   - Add `C:\Users\DELL G15\Documents\Experiments\GPT` to exclusions
3. Use WSL 2 backend instead of Hyper-V

#### Issue 5: "File permission errors"
```powershell
# Reset Docker to factory defaults
# Docker Desktop → Troubleshoot → Reset to factory defaults

# Or manually reset
wsl --shutdown
wsl --unregister docker-desktop
wsl --unregister docker-desktop-data
# Then restart Docker Desktop
```

### 📊 Service Access on Windows

After starting services, access them at:

| Service | URL | Default Credentials |
|---------|-----|-------------------|
| **API** | http://localhost:5000 | N/A |
| **Swagger UI** | http://localhost:5000/swagger | N/A |
| **MinIO Console** | http://localhost:9001 | admin / admin12345 |
| **PostgreSQL** | localhost:5432 | postgres / postgres |
| **pgAdmin** (if enabled) | http://localhost:5050 | admin@excursion.com / admin123 |

### 💻 PowerShell Management Script

Create `start-dev.ps1` for easy management:

```powershell
# Excursion GPT Windows Startup Script
Write-Host "Starting Excursion GPT Development Environment..." -ForegroundColor Green

# Check if Docker is running
if (-not (Get-Process -Name "Docker Desktop" -ErrorAction SilentlyContinue)) {
    Write-Host "Starting Docker Desktop..." -ForegroundColor Yellow
    Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    Start-Sleep -Seconds 30
}

# Navigate to project directory
Set-Location "C:\Users\DELL G15\Documents\Experiments\GPT\Excursion_GPT"

# Start services
Write-Host "Starting Docker Compose services..." -ForegroundColor Cyan
docker-compose -f docker-compose.dev.yml up -d

# Wait for services to start
Write-Host "Waiting for services to initialize..." -ForegroundColor Cyan
Start-Sleep -Seconds 15

# Show status
Write-Host "`nService Status:" -ForegroundColor Green
docker-compose -f docker-compose.dev.yml ps

# Show access information
Write-Host "`nAccess Information:" -ForegroundColor Green
Write-Host "API: http://localhost:5000" -ForegroundColor Yellow
Write-Host "Swagger: http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host "MinIO Console: http://localhost:9001" -ForegroundColor Yellow
Write-Host "`nMinIO Credentials: admin / admin12345" -ForegroundColor Cyan
Write-Host "PostgreSQL: postgres / postgres" -ForegroundColor Cyan

Write-Host "`nDevelopment environment is ready!" -ForegroundColor Green
```

### 🔄 Useful Windows Commands

#### Docker Management
```powershell
# List all containers
docker ps -a

# View logs
docker logs excursion-gpt-api-dev

# Execute commands in container
docker exec -it excursion-gpt-api-dev bash

# Stop all services
docker-compose down

# Remove everything
docker-compose down -v --rmi all

# Clean Docker system
docker system prune -a
```

#### WSL 2 Management
```powershell
# List WSL distributions
wsl --list --verbose

# Start WSL distribution
wsl -d Ubuntu

# Stop WSL
wsl --shutdown

# Export/import WSL distribution
wsl --export Ubuntu ubuntu_backup.tar
wsl --import UbuntuNew .\ubuntu_backup.tar
```

#### Network Diagnostics
```powershell
# Check port usage
netstat -ano | findstr :5000

# Test connectivity
Test-NetConnection -ComputerName localhost -Port 5000

# Flush DNS
ipconfig /flushdns
```

### 🛠️ Development Workflow on Windows

#### 1. Start Development Environment
```powershell
.\start-dev.ps1
# or
docker-compose -f docker-compose.dev.yml up -d
```

#### 2. Make Code Changes
- Edit files in Visual Studio or VS Code
- Changes are automatically detected in some configurations
- For manual rebuild: `docker-compose build api`

#### 3. Test API
```powershell
# Test health endpoint
curl http://localhost:5000/health

# Or using PowerShell
Invoke-RestMethod -Uri "http://localhost:5000/health"
```

#### 4. View Logs
```powershell
# Follow API logs
docker-compose logs -f api

# View all logs
docker-compose logs
```

#### 5. Stop Environment
```powershell
docker-compose down

# Preserve data volumes
docker-compose down
```

### 📈 Performance Optimization for Windows

1. **Use WSL 2 Backend**: Better performance than Hyper-V
2. **Exclude Project Folders** from Windows Defender real-time scanning
3. **Increase Docker Resources**:
   - RAM: 8GB minimum, 16GB recommended
   - CPUs: 4+ cores
   - Disk image size: 64GB+
4. **Store Docker Data in WSL 2**:
   - Docker Desktop → Settings → Resources → WSL Integration
   - Enable integration with your distribution
5. **Use SSD Storage**: Project on SSD improves performance significantly

### 🔒 Security Considerations for Windows

1. **Use Strong Passwords** in `env.config`
2. **Don't Expose Ports** unnecessarily
3. **Regular Updates**:
   ```powershell
   # Update Docker Desktop
   # Check for updates in Docker Desktop menu
   
   # Update WSL
   wsl --update
   ```
4. **Backup Important Data**:
   ```powershell
   # Backup database
   docker exec excursion-gpt-postgres-dev pg_dump -U postgres 3D_Excursion > backup.sql
   ```

### 🆘 Troubleshooting Checklist

✅ **Docker Desktop running** (icon in system tray)  
✅ **WSL 2 installed and running** (`wsl --list --verbose`)  
✅ **Sufficient resources allocated** (Settings → Resources)  
✅ **Ports not in use** (`netstat -ano | findstr :PORT`)  
✅ **Virtualization enabled** in BIOS/UEFI  
✅ **Windows features enabled** (Hyper-V, Virtual Machine Platform)  
✅ **Project path accessible** from WSL 2  

### 📞 Getting Help

1. **Docker Desktop Logs**: 
   - Click whale icon → Troubleshoot → View logs
2. **WSL Logs**: `wsl --system`
3. **Event Viewer**: Check Windows logs for Docker/WSL issues
4. **Community Resources**:
   - Docker Desktop for Windows documentation
   - WSL GitHub repository
   - Stack Overflow (tag: docker-windows)

### 🎯 Success Indicators

Your Windows Docker setup is successful when:

1. ✅ Docker Desktop runs without errors
2. ✅ `docker --version` shows version info
3. ✅ `docker-compose up -d` starts all services
4. ✅ http://localhost:5000/health returns `{"status":"Healthy"}`
5. ✅ http://localhost:5000/swagger loads API documentation
6. ✅ http://localhost:9001 loads MinIO console

---

**Note for Windows Users**: 
- Always run PowerShell as Administrator for system-level commands
- Use WSL 2 for best Linux container compatibility
- Keep Docker Desktop and WSL updated regularly
- Monitor resource usage in Task Manager

**Last Updated**: 2024-01-01

For additional Windows-specific help, consult the Docker Desktop for Windows documentation.