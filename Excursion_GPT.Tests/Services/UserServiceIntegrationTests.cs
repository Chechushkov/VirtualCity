using System;
using System.Threading.Tasks;
using AutoMapper;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Services;
using Excursion_GPT.Domain.Entities;
using Excursion_GPT.Domain.Enums;
using Excursion_GPT.Infrastructure.Data;
using Excursion_GPT.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Excursion_GPT.Tests.Services
{
    public class UserServiceIntegrationTests : TestBase, IDisposable
    {
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly UserService _userService;

        public UserServiceIntegrationTests()
        {
            // Configure AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, UserDto>();
                cfg.CreateMap<UserCreateDto, User>();
                cfg.CreateMap<UserUpdateDto, User>();
            });
            _mapper = config.CreateMapper();

            _passwordHasher = new PasswordHasher();
            _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
            _mockLogger = new Mock<ILogger<UserService>>();

            _userService = new UserService(_dbContext, _passwordHasher, _mockJwtTokenGenerator.Object, _mapper);
        }

        [Fact]
        public async Task CreateUserAsync_ValidData_CreatesUser()
        {
            // Arrange
            var userCreateDto = new UserCreateDto(
                "Test User",
                "testuser",
                "password123",
                "+1234567890",
                "Test School",
                Role.User
            );

            // Act
            var result = await _userService.CreateUserAsync(userCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userCreateDto.Name, result.Name);
            Assert.Equal(userCreateDto.Login, result.Login);
            Assert.Equal(userCreateDto.Phone, result.Phone);
            Assert.Equal(userCreateDto.SchoolName, result.SchoolName);
            Assert.Equal(userCreateDto.Role, result.Role);

            // Verify user was saved to database
            var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Login == userCreateDto.Login);
            Assert.NotNull(savedUser);
            Assert.Equal(userCreateDto.Name, savedUser.Name);
            Assert.True(_passwordHasher.VerifyPassword(savedUser.PasswordHash, userCreateDto.Password));
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            // Arrange
            // Clear existing users first to avoid seeded data interference
            _dbContext.Users.RemoveRange(_dbContext.Users);
            await _dbContext.SaveChangesAsync();

            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Name = "User 1", Login = "user1", PasswordHash = "hash1", Phone = "123", SchoolName = "School 1", Role = Role.User },
                new User { Id = Guid.NewGuid(), Name = "User 2", Login = "user2", PasswordHash = "hash2", Phone = "456", SchoolName = "School 2", Role = Role.Creator }
            };

            await _dbContext.Users.AddRangeAsync(users);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetUserByIdAsync_ValidId_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Name = "Test User",
                Login = "testuser",
                PasswordHash = "hash",
                Phone = "123",
                SchoolName = "Test School",
                Role = Role.User
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal(user.Name, result.Name);
            Assert.Equal(user.Login, result.Login);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var password = "password123";
            var hashedPassword = _passwordHasher.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Test User",
                Login = "testuser",
                PasswordHash = hashedPassword,
                Phone = "123",
                SchoolName = "Test School",
                Role = Role.User
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var loginDto = new UserLoginDto("testuser", password);
            var expectedToken = "test-jwt-token";
            _mockJwtTokenGenerator.Setup(x => x.GenerateToken(user.Id, user.Login, user.Role.ToString())).Returns(expectedToken);

            // Act
            var result = await _userService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(expectedToken, result.AccessToken);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Test User",
                Login = "testuser",
                PasswordHash = _passwordHasher.HashPassword("correctpassword"),
                Phone = "123",
                SchoolName = "Test School",
                Role = Role.User
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var loginDto = new UserLoginDto("testuser", "wrongpassword");

            // Act & Assert
            await Assert.ThrowsAsync<Excursion_GPT.Domain.Common.UnauthorizedException>(() =>
                _userService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_InvalidUsername_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new UserLoginDto("nonexistent", "password");

            // Act & Assert
            await Assert.ThrowsAsync<Excursion_GPT.Domain.Common.UnauthorizedException>(() =>
                _userService.LoginAsync(loginDto));
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}
