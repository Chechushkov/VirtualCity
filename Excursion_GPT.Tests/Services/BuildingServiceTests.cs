using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Excursion_GPT.Tests.Services
{
    public class BuildingServiceTests
    {
        private readonly Mock<ILogger<BuildingService>> _mockLogger;
        private readonly BuildingService _buildingService;

        public BuildingServiceTests()
        {
            _mockLogger = new Mock<ILogger<BuildingService>>();
            _buildingService = new BuildingService(null, _mockLogger.Object);
        }

        [Fact]
        public async Task GetBuildingsAroundPointAsync_ValidRequest_ReturnsBuildings()
        {
            // Arrange
            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 100.0
            };

            // Act
            var result = await _buildingService.GetBuildingsAroundPointAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            // Check that we have both standard and model buildings
            var standardBuildings = result.OfType<object>().Where(b =>
                b.GetType().GetProperty("nd") != null);
            var modelBuildings = result.OfType<object>().Where(b =>
                b.GetType().GetProperty("model") != null);

            Assert.True(standardBuildings.Any() || modelBuildings.Any());
        }

        [Fact]
        public async Task GetBuildingsAroundPointAsync_NorthPole_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 90.0, Z = 37.618423 }, // North pole
                Distance = 100.0
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _buildingService.GetBuildingsAroundPointAsync(request));
        }

        [Fact]
        public async Task GetBuildingsAroundPointAsync_SouthPole_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = -90.0, Z = 37.618423 }, // South pole
                Distance = 100.0
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _buildingService.GetBuildingsAroundPointAsync(request));
        }

        [Fact]
        public async Task GetBuildingsAroundPointAsync_ExtremeLongitude_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 181.0 }, // Invalid longitude
                Distance = 100.0
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _buildingService.GetBuildingsAroundPointAsync(request));
        }

        [Fact]
        public async Task GetBuildingsAroundPointAsync_ValidCoordinates_ReturnsMixedBuildingTypes()
        {
            // Arrange
            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 500.0
            };

            // Act
            var result = await _buildingService.GetBuildingsAroundPointAsync(request);

            // Assert
            Assert.NotNull(result);

            // Verify structure of standard buildings
            var standardBuildings = result.OfType<object>().Where(b =>
                b.GetType().GetProperty("nd") != null);

            foreach (var building in standardBuildings)
            {
                var id = building.GetType().GetProperty("id")?.GetValue(building);
                var nodes = building.GetType().GetProperty("nodes")?.GetValue(building) as IEnumerable<object>;

                Assert.NotNull(id);
                Assert.NotNull(nodes);
                Assert.NotEmpty(nodes);

                foreach (var node in nodes)
                {
                    var x = node.GetType().GetProperty("x")?.GetValue(node);
                    var z = node.GetType().GetProperty("z")?.GetValue(node);

                    Assert.NotNull(x);
                    Assert.NotNull(z);
                }
            }

            // Verify structure of model buildings
            var modelBuildings = result.OfType<object>().Where(b =>
                b.GetType().GetProperty("model") != null);

            foreach (var building in modelBuildings)
            {
                var id = building.GetType().GetProperty("id")?.GetValue(building);
                var model = building.GetType().GetProperty("model")?.GetValue(building);
                var x = building.GetType().GetProperty("x")?.GetValue(building);
                var z = building.GetType().GetProperty("z")?.GetValue(building);
                var rot = building.GetType().GetProperty("rot")?.GetValue(building) as IEnumerable<double>;

                Assert.NotNull(id);
                Assert.NotNull(model);
                Assert.NotNull(x);
                Assert.NotNull(z);
                Assert.NotNull(rot);
                Assert.Equal(3, rot.Count());
            }
        }

        [Fact]
        public async Task GetBuildingByAddressAsync_ValidAddress_ReturnsBuildingInfo()
        {
            // Arrange
            var request = new BuildingByAddressRequestDto
            {
                Address = "Test Address 123"
            };

            // Act
            var result = await _buildingService.GetBuildingByAddressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Address, result.Address);
            Assert.NotNull(result.Nodes);
            Assert.NotEmpty(result.Nodes);
            Assert.True(result.Height > 0);

            foreach (var node in result.Nodes)
            {
                Assert.True(node.X != 0 || node.Z != 0);
            }
        }

        [Fact]
        public async Task GetBuildingByAddressAsync_NotFoundAddress_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new BuildingByAddressRequestDto
            {
                Address = "notfound_address_xyz"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _buildingService.GetBuildingByAddressAsync(request));
        }

        [Fact]
        public async Task GetBuildingByAddressAsync_EmptyAddress_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new BuildingByAddressRequestDto
            {
                Address = ""
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _buildingService.GetBuildingByAddressAsync(request));
        }

        [Fact]
        public async Task GetBuildingByAddressAsync_AddressWithModel_ReturnsModelUrl()
        {
            // Arrange
            var request = new BuildingByAddressRequestDto
            {
                Address = "model_building_address"
            };

            // Act
            var result = await _buildingService.GetBuildingByAddressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ModelUrl);
            Assert.Contains("model", result.ModelUrl);
        }

        [Fact]
        public async Task GetBuildingByAddressAsync_AddressWithoutModel_ReturnsNullModelUrl()
        {
            // Arrange
            var request = new BuildingByAddressRequestDto
            {
                Address = "standard_building_address"
            };

            // Act
            var result = await _buildingService.GetBuildingByAddressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.ModelUrl);
        }

        [Fact]
        public async Task GetBuildingsAroundPointAsync_DifferentDistances_ReturnsAppropriateResults()
        {
            // Test with small distance
            var smallRequest = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 10.0
            };

            var smallResult = await _buildingService.GetBuildingsAroundPointAsync(smallRequest);
            Assert.NotNull(smallResult);

            // Test with large distance
            var largeRequest = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 1000.0
            };

            var largeResult = await _buildingService.GetBuildingsAroundPointAsync(largeRequest);
            Assert.NotNull(largeResult);
        }

        [Fact]
        public async Task GetBuildingsAroundPointAsync_ZeroDistance_ReturnsBuildings()
        {
            // Arrange
            var request = new BuildingsAroundPointRequestDto
            {
                Position = new PositionDto { X = 55.751244, Z = 37.618423 },
                Distance = 0.0
            };

            // Act
            var result = await _buildingService.GetBuildingsAroundPointAsync(request);

            // Assert
            Assert.NotNull(result);
        }
    }
}
