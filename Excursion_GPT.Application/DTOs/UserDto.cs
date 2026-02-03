using Excursion_GPT.Domain.Enums;

namespace Excursion_GPT.Application.DTOs;

public record UserDto(Guid Id,
    string Name,
    string Login,
    string Phone,
    string SchoolName,
    Role Role);
public record UserCreateDto(
    string Name,
    string Login,
    string Password,
    string Phone,
    string SchoolName,
    Role Role
);

public record UserUpdateDto(
    string? Name,
    string? Login,
    string? Password, // Optional password change
    string? Phone,
    string? SchoolName,
    Role? Role
);

public record UserLoginDto(
    string Login,
    string Password
);

public record AuthResponseDto(
    bool Success,
    string AccessToken
);

public record UserLogoutDto(
    string Login
);

public record RefreshTokenDto(
    string RefreshToken
);
