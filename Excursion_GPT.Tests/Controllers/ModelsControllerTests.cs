using System;
using System.Collections.Generic;
using System.IO;
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
    public class ModelsControllerTests
    {
        private readonly Mock<IModelService> _mockModelService;
        private readonly Mock<ILogger<ModelsController>> _mockLogger;
        private readonly ModelsController _controller;

        public ModelsControllerTests()
        {
            _mockModelService = new Mock<IModelService>();
            _mockLogger = new Mock<ILogger<ModelsController>>();
            _controller = new ModelsController(_mockModelService.Object, _mockLogger.Object);
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
        public async Task GetAllModels_AuthenticatedUser_ReturnsModels()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var models = new List<ModelDto>
            {
                new ModelDto(Guid.NewGuid(), new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 }, 1.0),
                new ModelDto(Guid.NewGuid(), new double[] { 10, 20, 30 }, new double[] { 45, 90, 0 }, 2.0)
            };

            _mockModelService.Setup(x => x.GetAllModelsAsync()).ReturnsAsync(models);

            // Act
            var result = await _controller.GetAllModels();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedModels = Assert.IsType<List<ModelDto>>(okResult.Value);
            Assert.Equal(2, returnedModels.Count);
            _mockModelService.Verify(x => x.GetAllModelsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetModelById_ValidId_ReturnsModel()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var modelId = Guid.NewGuid();
            var model = new ModelDto(modelId, new double[] { 10, 20, 30 }, new double[] { 45, 90, 0 }, 1.5);

            _mockModelService.Setup(x => x.GetModelByIdAsync(modelId)).ReturnsAsync(model);

            // Act
            var result = await _controller.GetModelById(modelId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedModel = Assert.IsType<ModelDto>(okResult.Value);
            Assert.Equal(modelId, returnedModel.Id);
            Assert.Equal(new double[] { 10, 20, 30 }, returnedModel.Position);
            Assert.Equal(new double[] { 45, 90, 0 }, returnedModel.Rotation);
            Assert.Equal(1.5, returnedModel.Scale);
            _mockModelService.Verify(x => x.GetModelByIdAsync(modelId), Times.Once);
        }

        [Fact]
        public async Task UploadModel_ValidData_ReturnsUploadResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var buildingId = Guid.NewGuid();
            var trackId = Guid.NewGuid();

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.glb");
            mockFile.Setup(f => f.ContentType).Returns("model/gltf-binary");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            var uploadDto = new ModelUploadRequestDto(trackId, mockFile.Object);
            var uploadResponse = new ModelUploadResponseDto(Guid.NewGuid().ToString());

            _mockModelService.Setup(x => x.UploadModelAsync(buildingId, uploadDto)).ReturnsAsync(uploadResponse);

            // Act
            var result = await _controller.UploadModel(buildingId, uploadDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResponse = Assert.IsType<ModelUploadResponseDto>(okResult.Value);
            Assert.Equal(uploadResponse.ModelId, returnedResponse.ModelId);
            _mockModelService.Verify(x => x.UploadModelAsync(buildingId, uploadDto), Times.Once);
        }

        [Fact]
        public async Task UpdateModelPosition_ValidData_ReturnsUpdatedModel()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var modelId = Guid.NewGuid();
            var updateDto = new ModelUpdateDto(
                new double[] { 15, 25, 35 },
                new double[] { 90, 45, 0 },
                2.0
            );

            var updatedModel = new ModelDto(modelId, updateDto.Position, updateDto.Rotation, updateDto.Scale);

            _mockModelService.Setup(x => x.UpdateModelPositionAsync(modelId, updateDto)).ReturnsAsync(updatedModel);

            // Act
            var result = await _controller.UpdateModelPosition(modelId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedModel = Assert.IsType<ModelDto>(okResult.Value);
            Assert.Equal(modelId, returnedModel.Id);
            Assert.Equal(new double[] { 15, 25, 35 }, returnedModel.Position);
            Assert.Equal(new double[] { 90, 45, 0 }, returnedModel.Rotation);
            Assert.Equal(2.0, returnedModel.Scale);
            _mockModelService.Verify(x => x.UpdateModelPositionAsync(modelId, updateDto), Times.Once);
        }

        [Fact]
        public async Task GetModelFile_ValidId_ReturnsFileStream()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var modelId = Guid.NewGuid();
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            var contentType = "model/gltf-binary";

            _mockModelService.Setup(x => x.GetModelFileAsync(modelId)).ReturnsAsync((stream, contentType));

            // Act
            var result = await _controller.GetModelFile(modelId);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal(stream, fileResult.FileStream);
            Assert.Equal(contentType, fileResult.ContentType);
            _mockModelService.Verify(x => x.GetModelFileAsync(modelId), Times.Once);
        }

        [Fact]
        public async Task DeleteModel_ValidId_ReturnsNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var modelId = Guid.NewGuid();

            _mockModelService.Setup(x => x.DeleteModelAsync(modelId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteModel(modelId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockModelService.Verify(x => x.DeleteModelAsync(modelId), Times.Once);
        }

        [Fact]
        public async Task GetAllModels_UserRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "User");

            var models = new List<ModelDto>
            {
                new ModelDto(Guid.NewGuid(), new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 }, 1.0)
            };

            _mockModelService.Setup(x => x.GetAllModelsAsync()).ReturnsAsync(models);

            // Act
            var result = await _controller.GetAllModels();

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockModelService.Verify(x => x.GetAllModelsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetModelById_CreatorRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var modelId = Guid.NewGuid();
            var model = new ModelDto(modelId, new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 }, 1.0);

            _mockModelService.Setup(x => x.GetModelByIdAsync(modelId)).ReturnsAsync(model);

            // Act
            var result = await _controller.GetModelById(modelId);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockModelService.Verify(x => x.GetModelByIdAsync(modelId), Times.Once);
        }

        [Fact]
        public async Task GetModelFile_AdminRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Admin");

            var modelId = Guid.NewGuid();
            var stream = new MemoryStream();
            var contentType = "model/gltf-binary";

            _mockModelService.Setup(x => x.GetModelFileAsync(modelId)).ReturnsAsync((stream, contentType));

            // Act
            var result = await _controller.GetModelFile(modelId);

            // Assert
            Assert.IsType<FileStreamResult>(result);
            _mockModelService.Verify(x => x.GetModelFileAsync(modelId), Times.Once);
        }

        [Fact]
        public async Task UploadModel_AdminRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Admin");

            var buildingId = Guid.NewGuid();
            var trackId = Guid.NewGuid();

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.glb");
            mockFile.Setup(f => f.ContentType).Returns("model/gltf-binary");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            var uploadDto = new ModelUploadRequestDto(trackId, mockFile.Object);
            var uploadResponse = new ModelUploadResponseDto(Guid.NewGuid().ToString());

            _mockModelService.Setup(x => x.UploadModelAsync(buildingId, uploadDto)).ReturnsAsync(uploadResponse);

            // Act
            var result = await _controller.UploadModel(buildingId, uploadDto);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockModelService.Verify(x => x.UploadModelAsync(buildingId, uploadDto), Times.Once);
        }

        [Fact]
        public async Task DeleteModel_AdminRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Admin");

            var modelId = Guid.NewGuid();

            _mockModelService.Setup(x => x.DeleteModelAsync(modelId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteModel(modelId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockModelService.Verify(x => x.DeleteModelAsync(modelId), Times.Once);
        }

        [Fact]
        public async Task GetAllModels_EmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var models = new List<ModelDto>();

            _mockModelService.Setup(x => x.GetAllModelsAsync()).ReturnsAsync(models);

            // Act
            var result = await _controller.GetAllModels();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedModels = Assert.IsType<List<ModelDto>>(okResult.Value);
            Assert.Empty(returnedModels);
            _mockModelService.Verify(x => x.GetAllModelsAsync(), Times.Once);
        }
    }
}
