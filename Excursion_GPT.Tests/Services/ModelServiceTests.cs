using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Application.Services;
using Excursion_GPT.Domain.Entities;
using Excursion_GPT.Infrastructure.Data;
using Excursion_GPT.Infrastructure.Minio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Excursion_GPT.Tests.Services
{
    public class ModelServiceTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<IMinioService> _mockMinioService;
        private readonly Mock<ILogger<ModelService>> _mockLogger;
        private readonly ModelService _modelService;

        public ModelServiceTests()
        {
            _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _mockMinioService = new Mock<IMinioService>();
            _mockLogger = new Mock<ILogger<ModelService>>();

            _modelService = new ModelService(
                _mockContext.Object,
                _mockMinioService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task UpdateModelPositionAsync_ValidModelId_UpdatesPositionSuccessfully()
        {
            // Arrange
            var modelId = Guid.NewGuid().ToString();
            var modelGuid = Guid.Parse(modelId);

            var existingModel = new Model
            {
                Id = modelGuid,
                BuildingId = Guid.NewGuid(),
                TrackId = Guid.NewGuid(),
                MinioObjectName = "test_model.stl",
                Position = new List<double> { 0, 0, 0 },
                Rotation = new List<double> { 0, 0, 0 },
                Scale = 1.0
            };

            var updateDto = new ModelUpdateRequestDto
            {
                Position = new List<double> { 10.5, 20.3, 30.7 },
                Rotation = new List<double> { 0.5, 1.2, 2.8 },
                Scale = 2.5
            };

            var mockDbSet = new Mock<DbSet<Model>>();
            mockDbSet.Setup(m => m.FindAsync(modelGuid))
                    .ReturnsAsync(existingModel);

            _mockContext.Setup(c => c.Models)
                       .Returns(mockDbSet.Object);

            // Act
            var result = await _modelService.UpdateModelPositionAsync(modelId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(modelId, result.Id);
            Assert.Equal(updateDto.Position, result.Position);
            Assert.Equal(updateDto.Rotation, result.Rotation);
            Assert.Equal(updateDto.Scale, result.Scale);

            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateModelPositionAsync_InvalidModelId_ThrowsException()
        {
            // Arrange
            var invalidModelId = "not-a-valid-guid";
            var updateDto = new ModelUpdateRequestDto
            {
                Position = new List<double> { 1, 2, 3 },
                Rotation = new List<double> { 0, 0, 0 },
                Scale = 1.0
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _modelService.UpdateModelPositionAsync(invalidModelId, updateDto));
        }

        [Fact]
        public async Task UpdateModelPositionAsync_ModelNotFound_ThrowsException()
        {
            // Arrange
            var modelId = Guid.NewGuid().ToString();
            var modelGuid = Guid.Parse(modelId);

            var updateDto = new ModelUpdateRequestDto
            {
                Position = new List<double> { 1, 2, 3 },
                Rotation = new List<double> { 0, 0, 0 },
                Scale = 1.0
            };

            var mockDbSet = new Mock<DbSet<Model>>();
            mockDbSet.Setup(m => m.FindAsync(modelGuid))
                    .ReturnsAsync((Model?)null);

            _mockContext.Setup(c => c.Models)
                       .Returns(mockDbSet.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _modelService.UpdateModelPositionAsync(modelId, updateDto));
        }

        [Fact]
        public async Task UpdateModelPositionAsync_InvalidPositionArray_ThrowsException()
        {
            // Arrange
            var modelId = Guid.NewGuid().ToString();

            var updateDto = new ModelUpdateRequestDto
            {
                Position = new List<double> { 1, 2 }, // Invalid: only 2 elements
                Rotation = new List<double> { 0, 0, 0 },
                Scale = 1.0
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _modelService.UpdateModelPositionAsync(modelId, updateDto));
        }

        [Fact]
        public async Task UpdateModelPositionAsync_InvalidRotationArray_ThrowsException()
        {
            // Arrange
            var modelId = Guid.NewGuid().ToString();

            var updateDto = new ModelUpdateRequestDto
            {
                Position = new List<double> { 1, 2, 3 },
                Rotation = new List<double> { 0, 0 }, // Invalid: only 2 elements
                Scale = 1.0
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _modelService.UpdateModelPositionAsync(modelId, updateDto));
        }

        [Fact]
        public async Task UpdateModelPositionAsync_DatabaseError_ThrowsException()
        {
            // Arrange
            var modelId = Guid.NewGuid().ToString();
            var modelGuid = Guid.Parse(modelId);

            var existingModel = new Model
            {
                Id = modelGuid,
                BuildingId = Guid.NewGuid(),
                TrackId = Guid.NewGuid(),
                MinioObjectName = "test_model.stl",
                Position = new List<double> { 0, 0, 0 },
                Rotation = new List<double> { 0, 0, 0 },
                Scale = 1.0
            };

            var updateDto = new ModelUpdateRequestDto
            {
                Position = new List<double> { 10.5, 20.3, 30.7 },
                Rotation = new List<double> { 0.5, 1.2, 2.8 },
                Scale = 2.5
            };

            var mockDbSet = new Mock<DbSet<Model>>();
            mockDbSet.Setup(m => m.FindAsync(modelGuid))
                    .ReturnsAsync(existingModel);

            _mockContext.Setup(c => c.Models)
                       .Returns(mockDbSet.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(default))
                       .ThrowsAsync(new DbUpdateException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _modelService.UpdateModelPositionAsync(modelId, updateDto));
        }
    }
}
