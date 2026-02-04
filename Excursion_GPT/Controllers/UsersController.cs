using System.Security.Claims;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Excursion_GPT.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Получить всех пользователей (только для администраторов)
    /// </summary>
    /// <remarks>
    /// **Ответ:**
    /// ```json
    /// [
    ///   {
    ///     "id": "guid",
    ///     "login": "string",
    ///     "email": "string",
    ///     "role": "Admin|Creator|User",
    ///     "createdAt": "datetime"
    ///   },
    ///   ...
    /// ]
    /// ```
    ///
    /// **Ошибки:**
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если пользователь не является администратором
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        _logger.LogInformation("Getting all users");
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "login": "string",
    ///   "email": "string",
    ///   "password": "string",
    ///   "role": "Admin|Creator|User"
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "guid",
    ///   "login": "string",
    ///   "email": "string",
    ///   "role": "Admin|Creator|User",
    ///   "createdAt": "datetime"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **400 Bad Request**: Если данные невалидны или пользователь уже существует
    /// </remarks>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] UserCreateDto userDto)
    {
        _logger.LogInformation("Creating new user");
        var createdUser = await _userService.CreateUserAsync(userDto);
        return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.Id }, createdUser);
    }

    /// <summary>
    /// Получить пользователя по ID (только для администраторов)
    /// </summary>
    /// <remarks>
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "guid",
    ///   "login": "string",
    ///   "email": "string",
    ///   "role": "Admin|Creator|User",
    ///   "createdAt": "datetime"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если пользователь не найден
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если пользователь не является администратором
    /// </remarks>
    [HttpGet("{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> GetUserById(Guid userId)
    {
        _logger.LogInformation("Getting user by ID: {UserId}", userId);
        var user = await _userService.GetUserByIdAsync(userId);
        return Ok(user);
    }

    /// <summary>
    /// Получить профиль текущего пользователя
    /// </summary>
    /// <remarks>
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "guid",
    ///   "login": "string",
    ///   "email": "string",
    ///   "role": "Admin|Creator|User",
    ///   "createdAt": "datetime"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если пользователь не найден
    /// - **401 Unauthorized**: Если не прилогинились
    /// </remarks>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUserProfile()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting current user profile for user ID: {UserId}", userId);
        var user = await _userService.GetUserByIdAsync(userId);
        return Ok(user);
    }

    /// <summary>
    /// Обновить данные пользователя (только для администраторов)
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "login": "string",
    ///   "email": "string",
    ///   "password": "string",
    ///   "role": "Admin|Creator|User"
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "guid",
    ///   "login": "string",
    ///   "email": "string",
    ///   "role": "Admin|Creator|User",
    ///   "createdAt": "datetime"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **400 Bad Request**: Если данные невалидны
    /// - **404 Not Found**: Если пользователь не найден
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если пользователь не является администратором
    /// </remarks>
    [HttpPut("{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid userId, [FromBody] UserUpdateDto userDto)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", userId);
        var updatedUser = await _userService.UpdateUserAsync(userId, userDto);
        return Ok(updatedUser);
    }

    /// <summary>
    /// Обновить профиль текущего пользователя
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "login": "string",
    ///   "email": "string",
    ///   "password": "string"
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "guid",
    ///   "login": "string",
    ///   "email": "string",
    ///   "role": "Admin|Creator|User",
    ///   "createdAt": "datetime"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **400 Bad Request**: Если данные невалидны
    /// - **404 Not Found**: Если пользователь не найден
    /// - **401 Unauthorized**: Если не прилогинились
    /// </remarks>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateCurrentUserProfile([FromBody] UserUpdateDto userDto)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Updating current user profile for user ID: {UserId}", userId);
        var updatedUser = await _userService.UpdateUserAsync(userId, userDto);
        return Ok(updatedUser);
    }

    /// <summary>
    /// Удалить пользователя (только для администраторов)
    /// </summary>
    /// <remarks>
    /// **Ошибки:**
    /// - **404 Not Found**: Если пользователь не найден
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если пользователь не является администратором
    /// </remarks>
    [HttpDelete("{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteUser(Guid userId)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", userId);
        await _userService.DeleteUserAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Аутентификация пользователя
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "login": "string",
    ///   "password": "string"
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "token": "jwt_token",
    ///   "expiresIn": 3600,
    ///   "user": {
    ///     "id": "guid",
    ///     "login": "string",
    ///     "email": "string",
    ///     "role": "Admin|Creator|User"
    ///   }
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **401 Unauthorized**: Если логин или пароль неверны
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] UserLoginDto loginDto)
    {
        _logger.LogInformation("Login attempt for user: {Login}", loginDto.Login);
        var response = await _userService.LoginAsync(loginDto);
        return Ok(response);
    }

    /// <summary>
    /// Выход пользователя (операция на стороне клиента)
    /// </summary>
    /// <remarks>
    /// Для JWT-аутентификации выход обычно обрабатывается на стороне клиента путем удаления токена.
    /// Этот endpoint может использоваться для очистки на стороне сервера, если это необходимо (например, черный список токенов).
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "message": "Logout successful. Please discard your token client-side.",
    ///   "userId": "guid"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **401 Unauthorized**: Если не прилогинились
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Logout request for user ID: {UserId}", userId);

        // For JWT tokens, logout is typically handled client-side by discarding the token.
        // If server-side token invalidation is needed, implement token blacklisting here.
        var logoutDto = new UserLogoutDto(GetCurrentUserLogin());
        await _userService.LogoutAsync(logoutDto);

        return Ok(new
        {
            success = true,
            message = "Logout successful. Please discard your token client-side.",
            userId = userId
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(subClaim, out var subUserId))
        {
            return subUserId;
        }

        throw new UnauthorizedAccessException("User ID not found in token.");
    }

    private string GetCurrentUserLogin()
    {
        var loginClaim = User.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(loginClaim))
        {
            return loginClaim;
        }

        throw new UnauthorizedAccessException("User login not found in token.");
    }
}
