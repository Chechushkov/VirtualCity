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
    public class TracksControllerTests
    {
        private readonly Mock<ITrackService> _mockTrackService;
        private readonly Mock<IPointService> _mockPointService;
        private readonly Mock<ILogger<TracksController>> _mockLogger;
        private readonly TracksController _controller;

        public TracksControllerTests()
        {
            _mockTrackService = new Mock<ITrackService>();
            _mockPointService = new Mock<IPointService>();
            _mockLogger = new Mock<ILogger<TracksController>>();
            _controller = new TracksController(_mockTrackService.Object, _mockPointService.Object, _mockLogger.Object);
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
        public async Task GetAllTracks_AuthenticatedUser_ReturnsTracks()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var tracks = new List<TrackDto>
            {
                new TrackDto(Guid.NewGuid(), "Track 1"),
                new TrackDto(Guid.NewGuid(), "Track 2")
            };

            _mockTrackService.Setup(x => x.GetAllTracksAsync()).ReturnsAsync(tracks);

            // Act
            var result = await _controller.GetAllTracks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTracks = Assert.IsType<List<TrackDto>>(okResult.Value);
            Assert.Equal(2, returnedTracks.Count);
            _mockTrackService.Verify(x => x.GetAllTracksAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTrackById_ValidId_ReturnsTrack()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var trackId = Guid.NewGuid();
            var trackDetails = new TrackDetailsDto(
                trackId,
                "Test Track",
                new List<PointDto>
                {
                    new PointDto(Guid.NewGuid(), "Point 1", "start", 0, 0, new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 }),
                    new PointDto(Guid.NewGuid(), "Point 2", "checkpoint", 10, 20, new double[] { 10, 20, 30 }, new double[] { 45, 90, 0 })
                }
            );

            _mockTrackService.Setup(x => x.GetTrackByIdAsync(trackId)).ReturnsAsync(trackDetails);

            // Act
            var result = await _controller.GetTrackById(trackId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTrack = Assert.IsType<TrackDetailsDto>(okResult.Value);
            Assert.Equal(trackId, returnedTrack.Id);
            Assert.Equal("Test Track", returnedTrack.Name);
            Assert.Equal(2, returnedTrack.Points.Count());
            _mockTrackService.Verify(x => x.GetTrackByIdAsync(trackId), Times.Once);
        }

        [Fact]
        public async Task CreateTrack_ValidData_ReturnsCreatedTrack()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var trackCreateDto = new TrackCreateDto("New Track");
            var createdTrack = new TrackDto(Guid.NewGuid(), trackCreateDto.Name);

            _mockTrackService.Setup(x => x.CreateTrackAsync(trackCreateDto, userId)).ReturnsAsync(createdTrack);

            // Act
            var result = await _controller.CreateTrack(trackCreateDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedTrack = Assert.IsType<TrackDto>(createdAtActionResult.Value);
            Assert.Equal(createdTrack.Id, returnedTrack.Id);
            Assert.Equal("New Track", returnedTrack.Name);
            _mockTrackService.Verify(x => x.CreateTrackAsync(trackCreateDto, userId), Times.Once);
        }

        [Fact]
        public async Task UpdateTrack_ValidData_ReturnsUpdatedTrack()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var trackId = Guid.NewGuid();
            var trackUpdateDto = new TrackUpdateDto("Updated Track Name");
            var updatedTrack = new TrackDto(trackId, trackUpdateDto.Name!);

            _mockTrackService.Setup(x => x.UpdateTrackAsync(trackId, trackUpdateDto)).ReturnsAsync(updatedTrack);

            // Act
            var result = await _controller.UpdateTrack(trackId, trackUpdateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTrack = Assert.IsType<TrackDto>(okResult.Value);
            Assert.Equal(trackId, returnedTrack.Id);
            Assert.Equal("Updated Track Name", returnedTrack.Name);
            _mockTrackService.Verify(x => x.UpdateTrackAsync(trackId, trackUpdateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteTrack_ValidId_ReturnsNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var trackId = Guid.NewGuid();

            _mockTrackService.Setup(x => x.DeleteTrackAsync(trackId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteTrack(trackId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTrackService.Verify(x => x.DeleteTrackAsync(trackId), Times.Once);
        }

        [Fact]
        public async Task AddPointToTrack_ValidData_ReturnsCreatedPoint()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var trackId = Guid.NewGuid();
            var pointCreateDto = new PointCreateDto(
                "New Point",
                "checkpoint",
                15,
                25,
                new double[] { 15, 25, 35 },
                new double[] { 90, 45, 0 }
            );

            var createdPoint = new PointDto(
                Guid.NewGuid(),
                pointCreateDto.Name,
                pointCreateDto.Type,
                pointCreateDto.Lat,
                pointCreateDto.Lng,
                pointCreateDto.Position,
                pointCreateDto.Rotation
            );

            _mockPointService.Setup(x => x.AddPointToTrackAsync(trackId, pointCreateDto)).ReturnsAsync(createdPoint);

            // Act
            var result = await _controller.AddPointToTrack(trackId, pointCreateDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedPoint = Assert.IsType<PointDto>(createdAtActionResult.Value);
            Assert.Equal(createdPoint.Id, returnedPoint.Id);
            Assert.Equal("New Point", returnedPoint.Name);
            Assert.Equal("checkpoint", returnedPoint.Type);
            _mockPointService.Verify(x => x.AddPointToTrackAsync(trackId, pointCreateDto), Times.Once);
        }

        [Fact]
        public async Task UpdatePoint_ValidData_ReturnsUpdatedPoint()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var trackId = Guid.NewGuid();
            var pointId = Guid.NewGuid();
            var pointUpdateDto = new PointUpdateDto(
                "Updated Point",
                "end",
                20,
                30,
                new double[] { 20, 30, 40 },
                new double[] { 180, 0, 0 }
            );

            var updatedPoint = new PointDto(
                pointId,
                pointUpdateDto.Name!,
                pointUpdateDto.Type!,
                pointUpdateDto.Lat!.Value,
                pointUpdateDto.Lng!.Value,
                pointUpdateDto.Position!,
                pointUpdateDto.Rotation!
            );

            _mockPointService.Setup(x => x.UpdatePointAsync(trackId, pointId, pointUpdateDto)).ReturnsAsync(updatedPoint);

            // Act
            var result = await _controller.UpdatePoint(trackId, pointId, pointUpdateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPoint = Assert.IsType<PointDto>(okResult.Value);
            Assert.Equal(pointId, returnedPoint.Id);
            Assert.Equal("Updated Point", returnedPoint.Name);
            Assert.Equal("end", returnedPoint.Type);
            _mockPointService.Verify(x => x.UpdatePointAsync(trackId, pointId, pointUpdateDto), Times.Once);
        }

        [Fact]
        public async Task DeletePoint_ValidId_ReturnsNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Creator");

            var trackId = Guid.NewGuid();
            var pointId = Guid.NewGuid();

            _mockPointService.Setup(x => x.DeletePointAsync(trackId, pointId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeletePoint(trackId, pointId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockPointService.Verify(x => x.DeletePointAsync(trackId, pointId), Times.Once);
        }

        [Fact]
        public async Task GetTrackPoints_ValidTrackId_ReturnsPoints()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var trackId = Guid.NewGuid();
            var points = new List<PointDto>
            {
                new PointDto(Guid.NewGuid(), "Start", "start", 0, 0, new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 }),
                new PointDto(Guid.NewGuid(), "Checkpoint", "checkpoint", 10, 20, new double[] { 10, 20, 30 }, new double[] { 45, 90, 0 }),
                new PointDto(Guid.NewGuid(), "End", "end", 20, 30, new double[] { 20, 30, 40 }, new double[] { 180, 0, 0 })
            };

            _mockPointService.Setup(x => x.GetPointsByTrackAsync(trackId)).ReturnsAsync(points);

            // Act
            var result = await _controller.GetTrackPoints(trackId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPoints = Assert.IsType<List<PointDto>>(okResult.Value);
            Assert.Equal(3, returnedPoints.Count);
            _mockPointService.Verify(x => x.GetPointsByTrackAsync(trackId), Times.Once);
        }

        [Fact]
        public async Task GetPointById_ValidIds_ReturnsPoint()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var trackId = Guid.NewGuid();
            var pointId = Guid.NewGuid();
            var point = new PointDto(pointId, "Test Point", "checkpoint", 15, 25, new double[] { 15, 25, 35 }, new double[] { 90, 45, 0 });

            _mockPointService.Setup(x => x.GetPointByIdAsync(trackId, pointId)).ReturnsAsync(point);

            // Act
            var result = await _controller.GetPointById(trackId, pointId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPoint = Assert.IsType<PointDto>(okResult.Value);
            Assert.Equal(pointId, returnedPoint.Id);
            Assert.Equal("Test Point", returnedPoint.Name);
            Assert.Equal("checkpoint", returnedPoint.Type);
            _mockPointService.Verify(x => x.GetPointByIdAsync(trackId, pointId), Times.Once);
        }

        [Fact]
        public async Task GetAllTracks_UserRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "User");

            var tracks = new List<TrackDto>
            {
                new TrackDto(Guid.NewGuid(), "Track 1")
            };

            _mockTrackService.Setup(x => x.GetAllTracksAsync()).ReturnsAsync(tracks);

            // Act
            var result = await _controller.GetAllTracks();

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockTrackService.Verify(x => x.GetAllTracksAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateTrack_AdminRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Admin");

            var trackCreateDto = new TrackCreateDto("New Track");
            var createdTrack = new TrackDto(Guid.NewGuid(), trackCreateDto.Name);

            _mockTrackService.Setup(x => x.CreateTrackAsync(trackCreateDto, userId)).ReturnsAsync(createdTrack);

            // Act
            var result = await _controller.CreateTrack(trackCreateDto);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result.Result);
            _mockTrackService.Verify(x => x.CreateTrackAsync(trackCreateDto, userId), Times.Once);
        }

        [Fact]
        public async Task DeleteTrack_AdminRole_AllowsAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "Admin");

            var trackId = Guid.NewGuid();

            _mockTrackService.Setup(x => x.DeleteTrackAsync(trackId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteTrack(trackId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTrackService.Verify(x => x.DeleteTrackAsync(trackId), Times.Once);
        }

        [Fact]
        public async Task GetAllTracks_EmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var tracks = new List<TrackDto>();

            _mockTrackService.Setup(x => x.GetAllTracksAsync()).ReturnsAsync(tracks);

            // Act
            var result = await _controller.GetAllTracks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTracks = Assert.IsType<List<TrackDto>>(okResult.Value);
            Assert.Empty(returnedTracks);
            _mockTrackService.Verify(x => x.GetAllTracksAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTrackPoints_EmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var trackId = Guid.NewGuid();
            var points = new List<PointDto>();

            _mockPointService.Setup(x => x.GetPointsByTrackAsync(trackId)).ReturnsAsync(points);

            // Act
            var result = await _controller.GetTrackPoints(trackId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPoints = Assert.IsType<List<PointDto>>(okResult.Value);
            Assert.Empty(returnedPoints);
            _mockPointService.Verify(x => x.GetPointsByTrackAsync(trackId), Times.Once);
        }
    }
}
