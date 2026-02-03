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
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);
        }

        private void SetupAuthenticatedUser(Guid userId, string login = "testuser")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, login),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private void SetupAdminUser(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetAllUsers_AdminUser_ReturnsUsers()
        {
            // Arrange
            var adminUserId = Guid.NewGuid();
            SetupAdminUser(adminUserId);

            var users = new List<UserDto>
            {
                new UserDto(Guid.NewGuid(), "User1", "user1", "1234567890", "School1", Domain.Enums.Role.User),
                new UserDto(Guid.NewGuid(), "User2", "user2", "0987654321", "School2", Domain.Enums.Role.Creator)
            };

            _mockUserService.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(users);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsType<List<UserDto>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count);
            _mockUserService.Verify(x => x.GetAllUsersAsync(), Times.Once);
        }

        [Fact]
        public async Task GetUserById_ValidId_ReturnsUser()
        {
            // Arrange
            var adminUserId = Guid.NewGuid();
            SetupAdminUser(adminUserId);

            var userId = Guid.NewGuid();
            var user = new UserDto(userId, "Test User", "testuser", "1234567890", "Test School", Domain.Enums.Role.User);

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
            Assert.Equal("Test User", returnedUser.Name);
            _mockUserService.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserProfile_AuthenticatedUser_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var user = new UserDto(userId, "Test User", "testuser", "1234567890", "Test School", Domain.Enums.Role.User);

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _controller.GetCurrentUserProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
            _mockUserService.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ValidData_ReturnsCreatedUser()
        {
            // Arrange
            var userCreateDto = new UserCreateDto(
                "New User",
                "newuser",
                "password123",
                "1234567890",
                "New School",
                Domain.Enums.Role.User
            );

            var createdUser = new UserDto(
                Guid.NewGuid(),
                userCreateDto.Name,
                userCreateDto.Login,
                userCreateDto.Phone,
                userCreateDto.SchoolName,
                userCreateDto.Role
            );

            _mockUserService.Setup(x => x.CreateUserAsync(userCreateDto)).ReturnsAsync(createdUser);

            // Act
            var result = await _controller.CreateUser(userCreateDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(createdAtActionResult.Value);
            Assert.Equal(createdUser.Id, returnedUser.Id);
            Assert.Equal("New User", returnedUser.Name);
            _mockUserService.Verify(x => x.CreateUserAsync(userCreateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ValidData_ReturnsUpdatedUser()
        {
            // Arrange
            var adminUserId = Guid.NewGuid();
            SetupAdminUser(adminUserId);

            var userId = Guid.NewGuid();
            var userUpdateDto = new UserUpdateDto(
                "Updated Name",
                null,
                null,
                "0987654321",
                "Updated School",
                Domain.Enums.Role.Creator
            );

            var updatedUser = new UserDto(
                userId,
                userUpdateDto.Name!,
                "testuser",
                userUpdateDto.Phone!,
                userUpdateDto.SchoolName!,
                userUpdateDto.Role!.Value
            );

            _mockUserService.Setup(x => x.UpdateUserAsync(userId, userUpdateDto)).ReturnsAsync(updatedUser);

            // Act
            var result = await _controller.UpdateUser(userId, userUpdateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
            Assert.Equal("Updated Name", returnedUser.Name);
            Assert.Equal("0987654321", returnedUser.Phone);
            _mockUserService.Verify(x => x.UpdateUserAsync(userId, userUpdateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_ValidId_ReturnsNoContent()
        {
            // Arrange
            var adminUserId = Guid.NewGuid();
            SetupAdminUser(adminUserId);

            var userId = Guid.NewGuid();

            _mockUserService.Setup(x => x.DeleteUserAsync(userId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockUserService.Verify(x => x.DeleteUserAsync(userId), Times.Once);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var loginDto = new UserLoginDto("testuser", "password123");
            var authResponse = new AuthResponseDto(true, "jwt-token-here");

            _mockUserService.Setup(x => x.LoginAsync(loginDto)).ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedAuth = Assert.IsType<AuthResponseDto>(okResult.Value);
            Assert.True(returnedAuth.Success);
            Assert.Equal("jwt-token-here", returnedAuth.AccessToken);
            _mockUserService.Verify(x => x.LoginAsync(loginDto), Times.Once);
        }

        [Fact]
        public async Task Logout_AuthenticatedUser_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "testuser");

            var logoutDto = new UserLogoutDto("testuser");
            _mockUserService.Setup(x => x.LogoutAsync(logoutDto)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            _mockUserService.Verify(x => x.LogoutAsync(It.Is<UserLogoutDto>(d => d.Login == "testuser")), Times.Once);
        }

        [Fact]
        public async Task UpdateCurrentUserProfile_ValidData_ReturnsUpdatedUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var userUpdateDto = new UserUpdateDto(
                "Updated Name",
                null,
                "newpassword",
                "0987654321",
                "Updated School",
                null
            );

            var updatedUser = new UserDto(
                userId,
                userUpdateDto.Name!,
                "testuser",
                userUpdateDto.Phone!,
                userUpdateDto.SchoolName!,
                Domain.Enums.Role.User
            );

            _mockUserService.Setup(x => x.UpdateUserAsync(userId, userUpdateDto)).ReturnsAsync(updatedUser);

            // Act
            var result = await _controller.UpdateCurrentUserProfile(userUpdateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
            Assert.Equal("Updated Name", returnedUser.Name);
            _mockUserService.Verify(x => x.UpdateUserAsync(userId, userUpdateDto), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserId_ValidClaims_ReturnsUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId);

            var user = new UserDto(userId, "Test User", "testuser", "1234567890", "Test School", Domain.Enums.Role.User);
            _mockUserService.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act & Assert - This tests the private method indirectly through public methods
            var result = await _controller.GetCurrentUserProfile();

            // The method should not throw an exception
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetCurrentUserLogin_ValidClaims_ReturnsLogin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupAuthenticatedUser(userId, "testuser");

            var logoutDto = new UserLogoutDto("testuser");
            _mockUserService.Setup(x => x.LogoutAsync(logoutDto)).Returns(Task.CompletedTask);

            // Act & Assert - This tests the private method indirectly through public methods
            var result = await _controller.Logout();

            // The method should not throw an exception
            Assert.NotNull(result);
        }
    }
}
