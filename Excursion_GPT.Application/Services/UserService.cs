using AutoMapper;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Domain.Common;
using Excursion_GPT.Domain.Entities;
using Excursion_GPT.Infrastructure.Data;
using Excursion_GPT.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using InvalidOperationException = System.InvalidOperationException;

namespace Excursion_GPT.Application.Services;

public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IMapper _mapper;

        public UserService(AppDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IMapper mapper)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new NotFoundException("user", "User not found");
            }
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateUserAsync(UserCreateDto userDto)
        {
            if (await _context.Users.AnyAsync(u => u.Login == userDto.Login))
            {
                throw new InvalidOperationException("user", new Exception("User with this login already exists."));
            }

            var user = _mapper.Map<User>(userDto);
            user.Id = Guid.NewGuid();
            user.PasswordHash = _passwordHasher.HashPassword(userDto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UserUpdateDto userDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new NotFoundException("user", "User not found");
            }

            if (!string.IsNullOrEmpty(userDto.Login) && userDto.Login != user.Login &&
                await _context.Users.AnyAsync(u => u.Login == userDto.Login))
            {
                throw new InvalidOperationException("user", new Exception("User with this login already exists."));
            }

            _mapper.Map(userDto, user);

            if (!string.IsNullOrEmpty(userDto.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(userDto.Password);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new NotFoundException("user", "User not found");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<AuthResponseDto> LoginAsync(UserLoginDto loginDto)
        {
            var con = await _context.Database.CanConnectAsync();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == loginDto.Login);
            if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, loginDto.Password))
            {
                throw new UnauthorizedException("credentials", "Invalid login or password");
            }

            var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Login, user.Role.ToString());
            return new AuthResponseDto(true, token);
        }

        public Task LogoutAsync(UserLogoutDto logoutDto)
        {
            // For JWT, logout is typically handled client-side by discarding the token.
            // If server-side token invalidation is needed (e.g., for refresh tokens),
            // it would be implemented here (e.g., add to a blacklist).
            // For now, we just return a completed task.
            return Task.CompletedTask;
        }
    }