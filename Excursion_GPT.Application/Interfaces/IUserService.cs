using Excursion_GPT.Application.DTOs;

namespace Excursion_GPT.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> GetUserByIdAsync(Guid id);
    Task<UserDto> CreateUserAsync(UserCreateDto userDto);
    Task<UserDto> UpdateUserAsync(Guid id, UserUpdateDto userDto);
    Task DeleteUserAsync(Guid id);
    Task<AuthResponseDto> LoginAsync(UserLoginDto loginDto);
    Task LogoutAsync(UserLogoutDto logoutDto); // Placeholder, actual logout is client-side
}