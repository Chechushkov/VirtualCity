# Excursion GPT API

A comprehensive ASP.NET Core 9.0 API for managing 3D building excursions with Docker containerization support.

## 📋 Overview

Excursion GPT API is a multi-layer ASP.NET Core application that provides a complete backend for managing 3D building models, excursions (tracks), and user authentication. The API follows clean architecture principles and is fully containerized with Docker Compose.

## 🚀 Features

- **Complete API** according to `api.md` requirements
- **JWT Authentication** with role-based authorization (Admin, Creator, User)
- **Multi-layer Architecture** (Domain, Application, Infrastructure, Presentation)
- **Docker Compose** with PostgreSQL, MinIO, and Nginx
- **Swagger/OpenAPI** documentation
- **Comprehensive Error Handling** with standardized error responses
- **Unit & Integration Tests**
- **Health Checks** and monitoring endpoints
- **Auto-migrations** and database seeding on startup

## 🏗️ Architecture

```
Excursion_GPT/
├── Excursion_GPT/              # Web API (Presentation Layer)
├── Excursion_GPT.Application/  # Application Layer (Services, DTOs, Interfaces)
├── Excursion_GPT.Domain/       # Domain Layer (Entities, Enums, Exceptions)
├── Excursion_GPT.Infrastructure/ # Infrastructure Layer (Data, Security, MinIO)
└── Excursion_GPT.Tests/        # Test Projects
```

## 📁 Project Structure

```
GPT/
├── api.md                      # API requirements specification
├── Excursion_GPT/              # Main Web API project
│   ├── Controllers/           # API endpoints
│   ├── Extensions/            # DI and configuration extensions
│   ├── Middleware/            # Custom middleware
│   ├── Program.cs             # Application entry point
│   └── appsettings.json       # Configuration
├── docker-compose.yml         # Docker Compose configuration
└── README.md                  # This file
```

## 🔧 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL](https://www.postgresql.org/) (optional, Docker included)
- [MinIO](https://min.io/) (optional, Docker included)

## 🚀 Quick Start

### Option 1: Docker Compose (Recommended)

```bash
# Navigate to the project directory
cd Excursion_GPT

# Start all services
docker-compose up -d

# Check services status
docker-compose ps

# View logs
docker-compose logs -f api
```

### Option 2: Local Development

```bash
# Restore dependencies
dotnet restore

# Run database migrations
dotnet ef database update --project Excursion_GPT.Infrastructure

# Run the application
dotnet run --project Excursion_GPT
```

## 📡 API Endpoints

### Authentication
- `POST /api/Users/login` - User login
- `POST /api/Users` - Create new user
- `GET /api/Users` - Get all users (Admin only)

### Buildings
- `PUT /buildings` - Get buildings around a point
- `PUT /buildings/address` - Get building by address

### Models
- `POST /upload` - Upload 3D model
- `PUT /model/{model_id}` - Update model position
- `GET /model/{model_id}` - Get model file
- `PUT /models/address` - Get model metadata by address
- `PATCH /models/{model_id}` - Save model metadata

### Tracks (Excursions)
- `GET /tracks/` - Get all tracks
- `GET /tracks/{track_id}` - Get track with points
- `POST /tracks` - Create new track
- `DELETE /tracks/{track_id}` - Delete track

### Points in Tracks
- `POST /tracks/{track_id}` - Add point to track
- `PUT /tracks/{track_id}/{point_id}` - Update point
- `DELETE /tracks/{track_id}/{point_id}` - Delete point

## 🔐 Authentication

All endpoints require JWT authentication except:
- `POST /api/Users/login`
- `POST /api/Users` (user registration)

### Default Test User
```json
{
  "login": "admin",
  "password": "adminpass"
}
```

### Getting a Token
```bash
curl -X POST http://localhost:5000/api/Users/login \
  -H "Content-Type: application/json" \
  -d '{"login": "admin", "password": "adminpass"}'
```

### Using the Token
```bash
curl -X GET http://localhost:5000/api/Users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## 🐳 Docker Services

| Service | Port | Description |
|---------|------|-------------|
| API | 5000 | ASP.NET Core Web API |
| PostgreSQL | 5432 | Database |
| MinIO | 9000 | Object storage for 3D models |
| MinIO Console | 9001 | MinIO web interface |
| pgAdmin | 5050 | PostgreSQL web admin |
| Nginx | 80 | Reverse proxy (optional) |

## 🧪 Testing

### Run Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Excursion_GPT.Tests
```

### Test Credentials
- **Admin**: `admin` / `adminpass` (Role: Admin)
- **Creator**: `creator` / `creatorpass` (Role: Creator)

## ⚙️ Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT` - Development/Production
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `Jwt__Key` - JWT secret key
- `Jwt__Issuer` - JWT issuer
- `Jwt__Audience` - JWT audience
- `Minio__Endpoint` - MinIO endpoint
- `Minio__AccessKey` - MinIO access key
- `Minio__SecretKey` - MinIO secret key

### appsettings.json
```json
{
  "Jwt": {
    "Key": "YourSecretKeyHere",
    "Issuer": "ExcursionGPTApi",
    "Audience": "ExcursionGPTClients"
  },
  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "admin",
    "SecretKey": "admin12345",
    "BucketName": "models"
  }
}
```

## 📊 Database

### Entity Relationships
```
User (1) --- (Many) Track
Track (1) --- (Many) Point
Track (1) --- (Many) Model
Building (1) --- (1) Model (optional)
```

### Migrations
```bash
# Create new migration
dotnet ef migrations add MigrationName --project Excursion_GPT.Infrastructure

# Update database
dotnet ef database update --project Excursion_GPT.Infrastructure
```

## 🛠️ Development

### Build Docker Image
```bash
docker build -t excursion-gpt-api -f Excursion_GPT/Dockerfile .
```

### Run with Docker
```bash
docker run -p 5000:80 \
  -e ConnectionStrings__DefaultConnection="Host=localhost;..." \
  excursion-gpt-api
```

### Code Quality
```bash
# Format code
dotnet format

# Analyze code
dotnet analyze
```

## 🔍 Monitoring

### Health Check
```bash
curl http://localhost:5000/health
```

### Swagger UI
Open in browser: `http://localhost:5000`

### Database Admin
- **pgAdmin**: `http://localhost:5050` (admin@excursion.com / admin123)
- **MinIO Console**: `http://localhost:9001` (admin / admin12345)

## 🚨 Error Handling

The API returns standardized error responses:

```json
{
  "code": 404,
  "object": "building",
  "message": "Building not found"
}
```

### Common Error Codes
- `401` - Authentication required
- `403` - Access restricted (role-based)
- `404` - Resource not found
- `406` - Invalid position/terrain
- `413` - Upload failed

## 📈 Deployment

### Production Docker Compose
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### Environment Configuration
1. Copy `env.production.example` to `.env.production`
2. Update secrets and connection strings
3. Set `ASPNETCORE_ENVIRONMENT=Production`

### Security Considerations
- Change default JWT key in production
- Use HTTPS in production
- Set strong passwords for services
- Enable firewall rules
- Regular database backups

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit changes
4. Push to the branch
5. Create a Pull Request

### Development Guidelines
- Follow Clean Architecture principles
- Write unit tests for new features
- Update API documentation
- Maintain backward compatibility

## 📄 License

This project is proprietary software.

## 📞 Support

For issues and questions:
1. Check the [API Documentation](http://localhost:5000)
2. Review `api.md` for requirements
3. Check Docker logs for errors

## 🎯 Roadmap

- [ ] Real spatial queries for buildings
- [ ] Advanced search functionality
- [ ] File upload progress tracking
- [ ] WebSocket support for real-time updates
- [ ] Advanced user management
- [ ] Analytics and reporting
- [ ] Mobile app integration

---
**Happy Coding!** 🚀