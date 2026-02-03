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

        private void SetupAuthenticatedUser(string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
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
        public async Task GetBuildingsAroundPoint_AuthenticatedUser_ReturnsBuildings()
        {
            // Arrange
            SetupAuthenticatedUser("User");

            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 100.0
            };

            var expectedResult = new List<object>
            {
                new { id = "234234", nd = new[] { new { lat = 66.3333, lng = 65.4444 } } },
                new { id = "234235", model = "model_001", lat = 67.3333, lng = 68.4444, rot = new[] { 0.0, 1.5, 0.0 } }
            };

            _mockBuildingService.Setup(s => s.GetBuildingsAroundPointAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBuildings = Assert.IsType<List<object>>(okResult.Value);
            Assert.Equal(2, returnedBuildings.Count);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_Unauthenticated_ReturnsUnauthorized()
        {
            // Arrange - No user setup
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 100.0
            };

            // Act
            var result = await _controller.GetBuildingsAroundPoint(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_NorthPole_ReturnsNotAcceptable()
        {
            // Arrange
            SetupAuthenticatedUser("User");

            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 90.0, Z = 37.618423 }, // North pole
                Distance = 100.0
            };

            _mockBuildingService.Setup(s => s.GetBuildingsAroundPointAsync(request))
                .ThrowsAsync(new InvalidOperationException("Unknown terrain"));

            // Act
            var result = await _controller.GetBuildingsAroundPoint(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(406, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetBuildingByAddress_ValidAddress_ReturnsBuilding()
        {
            // Arrange
            SetupAuthenticatedUser("User");

            var request = new BuildingByAddressRequestDto
            {
                Address = "Test Address 123"
            };

            var expectedResponse = new BuildingByAddressResponseDto
            {
                Address = "Test Address 123",
                Nodes = new List<PositionDto>
                {
                    new PositionDto { X = 66.3333, Z = 65.4444 },
                    new PositionDto { X = 66.3334, Z = 65.4445 }
                },
                Height = 25.5,
                Position = new PositionDto { X = 66.3334, Z = 65.4445 }
            };

            _mockBuildingService.Setup(s => s.GetBuildingByAddressAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetBuildingByAddress(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBuilding = Assert.IsType<BuildingByAddressResponseDto>(okResult.Value);
            Assert.Equal("Test Address 123", returnedBuilding.Address);
            Assert.Equal(2, returnedBuilding.Nodes.Count);
            Assert.Equal(25.5, returnedBuilding.Height);
        }

        [Fact]
        public async Task GetBuildingByAddress_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser("User");

            var request = new BuildingByAddressRequestDto
            {
                Address = "notfound_address"
            };

            _mockBuildingService.Setup(s => s.GetBuildingByAddressAsync(request))
                .ThrowsAsync(new InvalidOperationException("Building not found"));

            // Act
            var result = await _controller.GetBuildingByAddress(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetBuildingByAddress_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupAuthenticatedUser("User");

            var request = new BuildingByAddressRequestDto
            {
                Address = "Test Address"
            };

            _mockBuildingService.Setup(s => s.GetBuildingByAddressAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.GetBuildingByAddress(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetBuildingByAddress_ForbiddenRole_ReturnsForbidden()
        {
            // Arrange
            SetupAuthenticatedUser("User");

            var request = new BuildingByAddressRequestDto
            {
                Address = "Test Address"
            };

            _mockBuildingService.Setup(s => s.GetBuildingByAddressAsync(request))
                .ThrowsAsync(new InvalidOperationException("role"));

            // Act
            var result = await _controller.GetBuildingByAddress(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_ServiceException_ReturnsAppropriateError()
        {
            // Arrange
            SetupAuthenticatedUser("User");

            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 100.0
            };

            _mockBuildingService.Setup(s => s.GetBuildingsAroundPointAsync(request))
                .ThrowsAsync(new InvalidOperationException("track"));

            // Act
            var result = await _controller.GetBuildingsAroundPoint(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetBuildingsAroundPoint_AllRolesAllowed()
        {
            // Test with User role
            await TestRoleAccess("User");

            // Test with Creator role
            await TestRoleAccess("Creator");

            // Test with Admin role
            await TestRoleAccess("Admin");
        }

        private async Task TestRoleAccess(string role)
        {
            // Arrange
            SetupAuthenticatedUser(role);

            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 100.0
            };

            var expectedResult = new List<object>
            {
                new { id = "test_building", nd = new[] { new { lat = 66.3333, lng = 65.4444 } } }
            };

            _mockBuildingService.Setup(s => s.GetBuildingsAroundPointAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetBuildingsAroundPoint(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetBuildingByAddress_AddressWithModel_ReturnsModelUrl()
        {
            // Arrange
            SetupAuthenticatedUser("User");

            var request = new BuildingByAddressRequestDto
            {
                Address = "model_building"
            };

            var expectedResponse = new BuildingByAddressResponseDto
            {
                Address = "model_building",
                Nodes = new List<PositionDto>
                {
                    new PositionDto { X = 66.3333, Z = 65.4444 }
                },
                Height = 30.0,
                Position = new PositionDto { X = 66.3333, Z = 65.4444 },
                ModelUrl = "https://storage.example.com/models/model_001.glb"
            };

            _mockBuildingService.Setup(s => s.GetBuildingByAddressAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetBuildingByAddress(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBuilding = Assert.IsType<BuildingByAddressResponseDto>(okResult.Value);
            Assert.NotNull(returnedBuilding.ModelUrl);
            Assert.Contains("model", returnedBuilding.ModelUrl);
        }
    }
}
