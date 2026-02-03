using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Excursion_GPT.Tests.Controllers
{
    public class BuildingsControllerTests
    {
        private readonly Mock<IBuildingService> _mockBuildingService;
        private readonly Mock<ILogger<BuildingsController>> _mockLogger;
        private readonly BuildingsController _controller;

        public BuildingsControllerTests()
        {
            _mockBuildingService = new Mock<IBuildingService>();
            _mockLogger = new Mock<ILogger<BuildingsController>>();
            _controller = new BuildingsController(_mockBuildingService.Object, _mockLogger.Object);
        }

        private void SetupAuthenticatedUser(Guid userId, string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_ValidParameters_ReturnsBuildings()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var latitude = 55.751244;
            var longitude = 37.618423;
            var excursionId = Guid.NewGuid();

            var buildings = new List<BuildingResponseDto>
            {
                new BuildingResponseDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Nd = new List<BuildingNodeDto>
                    {
                        new BuildingNodeDto(55.751244, 37.618423),
                        new BuildingNodeDto(55.751254, 37.618433)
                    }
                },
                new BuildingResponseDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Model = Guid.NewGuid().ToString(),
                    Lat = 55.755826,
                    Lng = 37.617300,
                    Rot = new List<double> { 0, 0, 0 }
                }
            };

            _mockBuildingService.Setup(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId))
                .ReturnsAsync(buildings);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(latitude, longitude, excursionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBuildings = Assert.IsType<List<BuildingResponseDto>>(okResult.Value);
            Assert.Equal(2, returnedBuildings.Count);
            _mockBuildingService.Verify(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId), Times.Once);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_UserRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "User");

            var latitude = 55.751244;
            var longitude = 37.618423;
            var excursionId = Guid.NewGuid();

            var buildings = new List<BuildingResponseDto>
            {
                new BuildingResponseDto { Id = Guid.NewGuid().ToString() }
            };

            _mockBuildingService.Setup(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId))
                .ReturnsAsync(buildings);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(latitude, longitude, excursionId);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockBuildingService.Verify(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId), Times.Once);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_CreatorRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var latitude = 55.751244;
            var longitude = 37.618423;
            var excursionId = Guid.NewGuid();

            var buildings = new List<BuildingResponseDto>
            {
                new BuildingResponseDto { Id = Guid.NewGuid().ToString() }
            };

            _mockBuildingService.Setup(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId))
                .ReturnsAsync(buildings);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(latitude, longitude, excursionId);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockBuildingService.Verify(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId), Times.Once);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_AdminRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Admin");

            var latitude = 55.751244;
            var longitude = 37.618423;
            var excursionId = Guid.NewGuid();

            var buildings = new List<BuildingResponseDto>
            {
                new BuildingResponseDto { Id = Guid.NewGuid().ToString() }
            };

            _mockBuildingService.Setup(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId))
                .ReturnsAsync(buildings);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(latitude, longitude, excursionId);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockBuildingService.Verify(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId), Times.Once);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_EmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var latitude = 55.751244;
            var longitude = 37.618423;
            var excursionId = Guid.NewGuid();

            var buildings = new List<BuildingResponseDto>();

            _mockBuildingService.Setup(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId))
                .ReturnsAsync(buildings);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(latitude, longitude, excursionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBuildings = Assert.IsType<List<BuildingResponseDto>>(okResult.Value);
            Assert.Empty(returnedBuildings);
            _mockBuildingService.Verify(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId), Times.Once);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_WithStandardBuildings_ReturnsCorrectData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var latitude = 55.751244;
            var longitude = 37.618423;
            var excursionId = Guid.NewGuid();

            var buildingId = Guid.NewGuid().ToString();
            var buildings = new List<BuildingResponseDto>
            {
                new BuildingResponseDto
                {
                    Id = buildingId,
                    Nd = new List<BuildingNodeDto>
                    {
                        new BuildingNodeDto(55.751244, 37.618423),
                        new BuildingNodeDto(55.751254, 37.618433),
                        new BuildingNodeDto(55.751244, 37.618433)
                    }
                }
            };

            _mockBuildingService.Setup(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId))
                .ReturnsAsync(buildings);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(latitude, longitude, excursionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBuildings = Assert.IsType<List<BuildingResponseDto>>(okResult.Value);
            var building = returnedBuildings[0];
            Assert.Equal(buildingId, building.Id);
            Assert.NotNull(building.Nd);
            Assert.Equal(3, building.Nd.Count());
            Assert.Null(building.Model);
            Assert.Null(building.Lat);
            Assert.Null(building.Lng);
            Assert.Null(building.Rot);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_WithCustomModels_ReturnsCorrectData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var latitude = 55.751244;
            var longitude = 37.618423;
            var excursionId = Guid.NewGuid();

            var buildingId = Guid.NewGuid().ToString();
            var modelId = Guid.NewGuid().ToString();
            var buildings = new List<BuildingResponseDto>
            {
                new BuildingResponseDto
                {
                    Id = buildingId,
                    Model = modelId,
                    Lat = 55.755826,
                    Lng = 37.617300,
                    Rot = new List<double> { 45.0, 90.0, 0.0 }
                }
            };

            _mockBuildingService.Setup(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId))
                .ReturnsAsync(buildings);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(latitude, longitude, excursionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBuildings = Assert.IsType<List<BuildingResponseDto>>(okResult.Value);
            var building = returnedBuildings[0];
            Assert.Equal(buildingId, building.Id);
            Assert.Equal(modelId, building.Model);
            Assert.Equal(55.755826, building.Lat);
            Assert.Equal(37.617300, building.Lng);
            Assert.NotNull(building.Rot);
            Assert.Equal(new double[] { 45.0, 90.0, 0.0 }, building.Rot);
            Assert.Null(building.Nd);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_MixedBuildingTypes_ReturnsCorrectData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var latitude = 55.751244;
            var longitude = 37.618423;
            var excursionId = Guid.NewGuid();

            var buildings = new List<BuildingResponseDto>
            {
                new BuildingResponseDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Nd = new List<BuildingNodeDto>
                    {
                        new BuildingNodeDto(55.751244, 37.618423)
                    }
                },
                new BuildingResponseDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Model = Guid.NewGuid().ToString(),
                    Lat = 55.755826,
                    Lng = 37.617300,
                    Rot = new List<double> { 0, 0, 0 }
                },
                new BuildingResponseDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Nd = new List<BuildingNodeDto>
                    {
                        new BuildingNodeDto(55.751254, 37.618433)
                    }
                }
            };

            _mockBuildingService.Setup(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId))
                .ReturnsAsync(buildings);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(latitude, longitude, excursionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBuildings = Assert.IsType<List<BuildingResponseDto>>(okResult.Value);
            Assert.Equal(3, returnedBuildings.Count);

            // Verify first building (standard)
            Assert.NotNull(returnedBuildings[0].Nd);
            Assert.Null(returnedBuildings[0].Model);

            // Verify second building (custom model)
            Assert.Null(returnedBuildings[1].Nd);
            Assert.NotNull(returnedBuildings[1].Model);

            // Verify third building (standard)
            Assert.NotNull(returnedBuildings[2].Nd);
            Assert.Null(returnedBuildings[2].Model);

            _mockBuildingService.Verify(x => x.GetBuildingsAroundPointAsync(latitude, longitude, excursionId), Times.Once);
        }
    }
}
