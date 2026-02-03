using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Services;
using Excursion_GPT.Domain.Common;
using Excursion_GPT.Domain.Entities;
using Excursion_GPT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using InvalidOperationException = System.InvalidOperationException;

namespace Excursion_GPT.Tests.Services
{
    public class BuildingServiceTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<BuildingService>> _mockLogger;
        private readonly BuildingService _buildingService;

        public BuildingServiceTests()
        {
            _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<BuildingService>>();

            _buildingService = new BuildingService(
                _mockContext.Object,
                _mockMapper.Object
            );
        }

        [Fact(Skip = "Disabled - Mocking EF async operations is problematic. Use integration tests instead.")]
        public async Task GetBuildingsAroundPointAsync_ValidCoordinates_ReturnsBuildings()
        {
            // Arrange
            var latitude = 55.751244;
            var longitude = 37.618423;
            var trackId = Guid.NewGuid();

            var buildings = new List<Building>
            {
                new Building { Id = Guid.NewGuid(), Latitude = 55.751244, Longitude = 37.618423 },
                new Building { Id = Guid.NewGuid(), Latitude = 55.755826, Longitude = 37.617300 }
            };

            var mockBuildingsSet = new Mock<DbSet<Building>>();
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.Provider).Returns(buildings.AsQueryable().Provider);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.Expression).Returns(buildings.AsQueryable().Expression);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.ElementType).Returns(buildings.AsQueryable().ElementType);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.GetEnumerator()).Returns(buildings.AsQueryable().GetEnumerator());

            var mockTracksSet = new Mock<DbSet<Track>>();
            var tracks = new List<Track> { new Track { Id = trackId } }.AsQueryable();
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Provider).Returns(tracks.Provider);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Expression).Returns(tracks.Expression);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.ElementType).Returns(tracks.ElementType);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.GetEnumerator()).Returns(tracks.GetEnumerator());

            _mockContext.Setup(c => c.Buildings).Returns(mockBuildingsSet.Object);
            _mockContext.Setup(c => c.Tracks).Returns(mockTracksSet.Object);

            var buildingResponseDtos = new List<BuildingResponseDto>
            {
                new BuildingResponseDto { Id = buildings[0].Id.ToString() },
                new BuildingResponseDto { Id = buildings[1].Id.ToString() }
            };

            _mockMapper.Setup(m => m.Map<IEnumerable<BuildingResponseDto>>(It.IsAny<IEnumerable<Building>>()))
                      .Returns(buildingResponseDtos);

            // Act
            var result = await _buildingService.GetBuildingsAroundPointAsync(latitude, longitude, trackId);

            // Assert
            Assert.Equal(2, result.Count());
            _mockContext.Verify(x => x.Buildings, Times.Once);
            _mockContext.Verify(x => x.Tracks, Times.Once);
        }

        [Fact(Skip = "Disabled - Mocking EF async operations is problematic. Use integration tests instead.")]
        public async Task GetBuildingsAroundPointAsync_InvalidLatitude_ThrowsInvalidOperationException()
        {
            // Arrange
            var latitude = 91.0; // Invalid latitude
            var longitude = 37.618423;
            var trackId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _buildingService.GetBuildingsAroundPointAsync(latitude, longitude, trackId));
        }

        [Fact(Skip = "Disabled - Mocking EF async operations is problematic. Use integration tests instead.")]
        public async Task GetBuildingsAroundPointAsync_InvalidLongitude_ThrowsInvalidOperationException()
        {
            // Arrange
            var latitude = 55.751244;
            var longitude = 181.0; // Invalid longitude
            var trackId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _buildingService.GetBuildingsAroundPointAsync(latitude, longitude, trackId));
        }

        [Fact(Skip = "Disabled - Mocking EF async operations is problematic. Use integration tests instead.")]
        public async Task GetBuildingsAroundPointAsync_PolarRegion_ThrowsInvalidOperationException()
        {
            // Arrange
            var latitude = 85.0; // Near pole
            var longitude = 37.618423;
            var trackId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _buildingService.GetBuildingsAroundPointAsync(latitude, longitude, trackId));
        }

        [Fact(Skip = "Disabled - Mocking EF async operations is problematic. Use integration tests instead.")]
        public async Task GetBuildingsAroundPointAsync_InvalidTrackId_ThrowsNotFoundException()
        {
            // Arrange
            var latitude = 55.751244;
            var longitude = 37.618423;
            var trackId = Guid.NewGuid();

            var mockTracksSet = new Mock<DbSet<Track>>();
            var tracks = new List<Track>().AsQueryable(); // Empty tracks list
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Provider).Returns(tracks.Provider);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Expression).Returns(tracks.Expression);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.ElementType).Returns(tracks.ElementType);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.GetEnumerator()).Returns(tracks.GetEnumerator());

            _mockContext.Setup(c => c.Tracks).Returns(mockTracksSet.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _buildingService.GetBuildingsAroundPointAsync(latitude, longitude, trackId));
        }

        [Fact(Skip = "Disabled - Mocking EF async operations is problematic. Use integration tests instead.")]
        public async Task GetBuildingsAroundPointAsync_WithCustomModels_ReturnsCorrectData()
        {
            // Arrange
            var latitude = 55.751244;
            var longitude = 37.618423;
            var trackId = Guid.NewGuid();

            var modelId = Guid.NewGuid();
            var buildingWithModel = new Building
            {
                Id = Guid.NewGuid(),
                Latitude = 55.755826,
                Longitude = 37.617300,
                ModelId = modelId,
                CustomModel = new Model
                {
                    Id = modelId,
                    MinioObjectName = "model.glb",
                    Position = new List<double> { 0, 0, 0 },
                    Rotation = new List<double> { 45, 90, 0 },
                    Scale = 1.0
                }
            };

            var buildings = new List<Building> { buildingWithModel };

            var mockBuildingsSet = new Mock<DbSet<Building>>();
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.Provider).Returns(buildings.AsQueryable().Provider);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.Expression).Returns(buildings.AsQueryable().Expression);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.ElementType).Returns(buildings.AsQueryable().ElementType);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.GetEnumerator()).Returns(buildings.AsQueryable().GetEnumerator());

            var mockTracksSet = new Mock<DbSet<Track>>();
            var tracks = new List<Track> { new Track { Id = trackId } }.AsQueryable();
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Provider).Returns(tracks.Provider);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Expression).Returns(tracks.Expression);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.ElementType).Returns(tracks.ElementType);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.GetEnumerator()).Returns(tracks.GetEnumerator());

            _mockContext.Setup(c => c.Buildings).Returns(mockBuildingsSet.Object);
            _mockContext.Setup(c => c.Tracks).Returns(mockTracksSet.Object);

            var buildingResponseDtos = new List<BuildingResponseDto>
            {
                new BuildingResponseDto
                {
                    Id = buildingWithModel.Id.ToString(),
                    Model = buildingWithModel.CustomModel.Id.ToString(),
                    Lat = buildingWithModel.Latitude,
                    Lng = buildingWithModel.Longitude,
                    Rot = buildingWithModel.CustomModel.Rotation
                }
            };

            _mockMapper.Setup(m => m.Map<IEnumerable<BuildingResponseDto>>(It.IsAny<IEnumerable<Building>>()))
                      .Returns(buildingResponseDtos);

            // Act
            var result = await _buildingService.GetBuildingsAroundPointAsync(latitude, longitude, trackId);

            // Assert
            var building = result.First();
            Assert.Equal(buildingWithModel.Id.ToString(), building.Id);
            Assert.Equal(modelId.ToString(), building.Model);
            Assert.Equal(55.755826, building.Lat);
            Assert.Equal(37.617300, building.Lng);
            Assert.Equal(new double[] { 45, 90, 0 }, building.Rot);
            Assert.Null(building.Nd);
        }

        [Fact(Skip = "Disabled - Mocking EF async operations is problematic. Use integration tests instead.")]
        public async Task GetBuildingsAroundPointAsync_WithStandardBuildings_ReturnsCorrectData()
        {
            // Arrange
            var latitude = 55.751244;
            var longitude = 37.618423;
            var trackId = Guid.NewGuid();

            var standardBuilding = new Building
            {
                Id = Guid.NewGuid(),
                Latitude = 55.751244,
                Longitude = 37.618423,
                ModelId = null,
                CustomModel = null
            };

            var buildings = new List<Building> { standardBuilding };

            var mockBuildingsSet = new Mock<DbSet<Building>>();
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.Provider).Returns(buildings.AsQueryable().Provider);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.Expression).Returns(buildings.AsQueryable().Expression);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.ElementType).Returns(buildings.AsQueryable().ElementType);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.GetEnumerator()).Returns(buildings.AsQueryable().GetEnumerator());

            var mockTracksSet = new Mock<DbSet<Track>>();
            var tracks = new List<Track> { new Track { Id = trackId } }.AsQueryable();
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Provider).Returns(tracks.Provider);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Expression).Returns(tracks.Expression);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.ElementType).Returns(tracks.ElementType);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.GetEnumerator()).Returns(tracks.GetEnumerator());

            _mockContext.Setup(c => c.Buildings).Returns(mockBuildingsSet.Object);
            _mockContext.Setup(c => c.Tracks).Returns(mockTracksSet.Object);

            var buildingResponseDtos = new List<BuildingResponseDto>
            {
                new BuildingResponseDto
                {
                    Id = standardBuilding.Id.ToString(),
                    Nd = new List<BuildingNodeDto>
                    {
                        new BuildingNodeDto(standardBuilding.Latitude + 0.01, standardBuilding.Longitude + 0.01),
                        new BuildingNodeDto(standardBuilding.Latitude - 0.01, standardBuilding.Longitude - 0.01)
                    }
                }
            };

            _mockMapper.Setup(m => m.Map<IEnumerable<BuildingResponseDto>>(It.IsAny<IEnumerable<Building>>()))
                      .Returns(buildingResponseDtos);

            // Act
            var result = await _buildingService.GetBuildingsAroundPointAsync(latitude, longitude, trackId);

            // Assert
            var building = result.First();
            Assert.Equal(standardBuilding.Id.ToString(), building.Id);
            Assert.NotNull(building.Nd);
            Assert.Equal(2, building.Nd.Count());
            Assert.Null(building.Model);
            Assert.Null(building.Lat);
            Assert.Null(building.Lng);
            Assert.Null(building.Rot);
        }

        [Fact(Skip = "Disabled - Mocking EF async operations is problematic. Use integration tests instead.")]
        public async Task GetBuildingsAroundPointAsync_NoBuildingsInArea_ReturnsEmptyList()
        {
            // Arrange
            var latitude = 55.751244;
            var longitude = 37.618423;
            var trackId = Guid.NewGuid();

            // Buildings far from the search point
            var buildings = new List<Building>
            {
                new Building { Id = Guid.NewGuid(), Latitude = 60.0, Longitude = 30.0 }, // Far away
                new Building { Id = Guid.NewGuid(), Latitude = 50.0, Longitude = 40.0 }  // Far away
            };

            var mockBuildingsSet = new Mock<DbSet<Building>>();
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.Provider).Returns(buildings.AsQueryable().Provider);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.Expression).Returns(buildings.AsQueryable().Expression);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.ElementType).Returns(buildings.AsQueryable().ElementType);
            mockBuildingsSet.As<IQueryable<Building>>().Setup(m => m.GetEnumerator()).Returns(buildings.AsQueryable().GetEnumerator());

            var mockTracksSet = new Mock<DbSet<Track>>();
            var tracks = new List<Track> { new Track { Id = trackId } }.AsQueryable();
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Provider).Returns(tracks.Provider);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.Expression).Returns(tracks.Expression);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.ElementType).Returns(tracks.ElementType);
            mockTracksSet.As<IQueryable<Track>>().Setup(m => m.GetEnumerator()).Returns(tracks.GetEnumerator());

            _mockContext.Setup(c => c.Buildings).Returns(mockBuildingsSet.Object);
            _mockContext.Setup(c => c.Tracks).Returns(mockTracksSet.Object);

            var buildingResponseDtos = new List<BuildingResponseDto>();

            _mockMapper.Setup(m => m.Map<IEnumerable<BuildingResponseDto>>(It.IsAny<IEnumerable<Building>>()))
                      .Returns(buildingResponseDtos);

            // Act
            var result = await _buildingService.GetBuildingsAroundPointAsync(latitude, longitude, trackId);

            // Assert
            Assert.Empty(result);
            _mockContext.Verify(x => x.Buildings, Times.Once);
            _mockContext.Verify(x => x.Tracks, Times.Once);
        }
    }
}
