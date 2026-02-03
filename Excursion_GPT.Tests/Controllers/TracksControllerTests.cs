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
            SetupAuthenticatedUser(Guid.NewGuid(), "User");

            var expectedTracks = new List<TrackListItemDto>
            {
                new TrackListItemDto { Id = "track_001", Name = "City Center Tour" },
                new TrackListItemDto { Id = "track_002", Name = "Historical Buildings" }
            };

            _mockTrackService.Setup(s => s.GetAllTracksAsync())
                .ReturnsAsync(expectedTracks);

            // Act
            var result = await _controller.GetAllTracks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTracks = Assert.IsType<List<TrackListItemDto>>(okResult.Value);
            Assert.Equal(2, returnedTracks.Count);
            Assert.Equal("track_001", returnedTracks[0].Id);
            Assert.Equal("City Center Tour", returnedTracks[0].Name);
        }

        [Fact]
        public async Task GetTrackById_ValidTrackId_ReturnsTrack()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "User");
            var trackId = "track_001";

            var expectedTrack = new TrackDetailsDto
            {
                Id = trackId,
                Name = "City Center Tour",
                Points = new List<PointDto>
                {
                    new PointDto
                    {
                        Id = "point_001",
                        Name = "Main Square",
                        Lat = 55.7558,
                        Lng = 37.6173,
                        Type = "viewpoint",
                        Position = new List<double> { 55.7558, 0.0, 37.6173 },
                        Rotation = new List<double> { 0.0, 0.0, 0.0 }
                    }
                }
            };

            _mockTrackService.Setup(s => s.GetTrackByIdAsync(trackId))
                .ReturnsAsync(expectedTrack);

            // Act
            var result = await _controller.GetTrackById(trackId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTrack = Assert.IsType<TrackDetailsDto>(okResult.Value);
            Assert.Equal(trackId, returnedTrack.Id);
            Assert.Equal("City Center Tour", returnedTrack.Name);
            Assert.Single(returnedTrack.Points);
        }

        [Fact]
        public async Task GetTrackById_NonExistentTrack_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "User");
            var trackId = "non_existent_track";

            _mockTrackService.Setup(s => s.GetTrackByIdAsync(trackId))
                .ThrowsAsync(new InvalidOperationException("Track not found"));

            // Act
            var result = await _controller.GetTrackById(trackId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task CreateTrack_AdminUser_CreatesTrack()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Admin");

            var createRequest = new TrackCreateRequestDto
            {
                Name = "New Excursion"
            };

            var expectedResponse = new TrackCreateResponseDto
            {
                Id = "new_track_001",
                Name = "New Excursion"
            };

            _mockTrackService.Setup(s => s.CreateTrackAsync(createRequest))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateTrack(createRequest);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedTrack = Assert.IsType<TrackCreateResponseDto>(createdResult.Value);
            Assert.Equal("new_track_001", returnedTrack.Id);
            Assert.Equal("New Excursion", returnedTrack.Name);
        }

        [Fact]
        public async Task CreateTrack_UnauthorizedUser_ReturnsForbidden()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "User"); // Not Admin or Creator

            var createRequest = new TrackCreateRequestDto
            {
                Name = "New Excursion"
            };

            // Act
            var result = await _controller.CreateTrack(createRequest);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(403, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task DeleteTrack_AdminUser_DeletesTrack()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Admin");
            var trackId = "track_001";

            _mockTrackService.Setup(s => s.DeleteTrackAsync(trackId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteTrack(trackId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteTrack_NonExistentTrack_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Admin");
            var trackId = "non_existent_track";

            _mockTrackService.Setup(s => s.DeleteTrackAsync(trackId))
                .ThrowsAsync(new InvalidOperationException("Track not found"));

            // Act
            var result = await _controller.DeleteTrack(trackId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddPointToTrack_CreatorUser_AddsPoint()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Creator");
            var trackId = "track_001";

            var pointRequest = new PointCreateRequestDto
            {
                Name = "New Point",
                Type = "viewpoint",
                Position = new List<double> { 55.7558, 0.0, 37.6173 },
                Rotation = new List<double> { 0.0, 0.0, 0.0 }
            };

            var expectedResponse = new PointCreateResponseDto
            {
                Id = "new_point_001"
            };

            _mockPointService.Setup(s => s.AddPointToTrackAsync(trackId, pointRequest))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AddPointToTrack(trackId, pointRequest);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedPoint = Assert.IsType<PointCreateResponseDto>(createdResult.Value);
            Assert.Equal("new_point_001", returnedPoint.Id);
        }

        [Fact]
        public async Task AddPointToTrack_NonExistentTrack_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Creator");
            var trackId = "non_existent_track";

            var pointRequest = new PointCreateRequestDto
            {
                Name = "New Point",
                Type = "viewpoint",
                Position = new List<double> { 55.7558, 0.0, 37.6173 },
                Rotation = new List<double> { 0.0, 0.0, 0.0 }
            };

            _mockPointService.Setup(s => s.AddPointToTrackAsync(trackId, pointRequest))
                .ThrowsAsync(new InvalidOperationException("Track not found"));

            // Act
            var result = await _controller.AddPointToTrack(trackId, pointRequest);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdatePoint_AdminUser_UpdatesPoint()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Admin");
            var trackId = "track_001";
            var pointId = "point_001";

            var updateRequest = new PointUpdateRequestDto
            {
                Name = "Updated Point Name",
                Position = new List<double> { 55.7560, 0.0, 37.6175 }
            };

            _mockPointService.Setup(s => s.UpdatePointAsync(trackId, pointId, updateRequest))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdatePoint(trackId, pointId, updateRequest);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdatePoint_NonExistentPoint_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Admin");
            var trackId = "track_001";
            var pointId = "non_existent_point";

            var updateRequest = new PointUpdateRequestDto
            {
                Name = "Updated Point Name"
            };

            _mockPointService.Setup(s => s.UpdatePointAsync(trackId, pointId, updateRequest))
                .ThrowsAsync(new InvalidOperationException("Point not found"));

            // Act
            var result = await _controller.UpdatePoint(trackId, pointId, updateRequest);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeletePoint_CreatorUser_DeletesPoint()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Creator");
            var trackId = "track_001";
            var pointId = "point_001";

            _mockPointService.Setup(s => s.DeletePointAsync(trackId, pointId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeletePoint(trackId, pointId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeletePoint_NonExistentPoint_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(Guid.NewGuid(), "Creator");
            var trackId = "track_001";
            var pointId = "non_existent_point";

            _mockPointService.Setup(s => s.DeletePointAsync(trackId, pointId))
                .ThrowsAsync(new InvalidOperationException("Point not found"));

            // Act
            var result = await _controller.DeletePoint(trackId, pointId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAllTracks_Unauthenticated_ReturnsUnauthorized()
        {
            // Arrange - No user setup
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetAllTracks();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.NotNull(unauthorizedResult.Value);
        }
    }
}
