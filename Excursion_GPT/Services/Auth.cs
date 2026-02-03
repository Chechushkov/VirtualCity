using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Excursion_GPT.Services;

public static class AuthSchemeConstants
{
    public const string AuthScheme = "AuthScheme";
    public const string AuthSchemeAny = "AuthScheme";
}

public class AuthSchemeOptions : AuthenticationSchemeOptions
{ }

public class AuthHandler : AuthenticationHandler<AuthSchemeOptions>
{
    private readonly IUserService _userService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IJwtTokenGenerator _tokenProcessor;

    public AuthHandler(
        IOptionsMonitor<AuthSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceProvider serviceProvider,
        IUserService userService,
        IJwtTokenGenerator tokenProcessor)
        : base(options, logger, encoder)
    {
        _serviceProvider = serviceProvider;
        _userService = userService;
        _tokenProcessor = tokenProcessor;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers;
        var token = headers.Authorization.ToString();

        if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer "))
        {
            return AuthenticateResult.Fail("Missing or invalid authorization header");
        }

        // Remove "Bearer " prefix
        token = token.Substring(7);

        try
        {
            var jwtSecurityToken = _tokenProcessor.ValidateToken(token);
            var jwtUserId = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;

            if (string.IsNullOrEmpty(jwtUserId) || !Guid.TryParse(jwtUserId, out var userId))
            {
                return AuthenticateResult.Fail("Invalid user ID in token");
            }

            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return AuthenticateResult.Fail("User not found");
            }

            // Extract actual user roles from the user object or token claims
            var userRoles = ExtractUserRoles(user, jwtSecurityToken);

            if (!userRoles.Any())
            {
                return AuthenticateResult.Fail("No roles assigned to user");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login ?? user.Id.ToString())
            };

            // Add actual user roles as claims
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add additional claims from the token if needed
            var additionalClaims = jwtSecurityToken.Claims
                .Where(c => c.Type != "userId" && c.Type != ClaimTypes.Role)
                .ToList();
            claims.AddRange(additionalClaims);

            var claimsIdentity = new ClaimsIdentity(claims, nameof(AuthHandler));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (SecurityTokenException ex)
        {
            return AuthenticateResult.Fail($"Token validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
        }
    }

    private List<string> ExtractUserRoles(object user, JwtSecurityToken jwtToken)
    {
        var roles = new List<string>();

        // First, try to get roles from the token claims
        var roleClaims = jwtToken.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        if (roleClaims.Any())
        {
            roles.AddRange(roleClaims);
        }
        else
        {
            // If no roles in token, try to extract from user object
            // This assumes the user object has a Roles property or similar
            var userType = user.GetType();
            var rolesProperty = userType.GetProperty("Roles") ??
                              userType.GetProperty("Role") ??
                              userType.GetProperty("UserRoles");

            if (rolesProperty != null)
            {
                var userRoles = rolesProperty.GetValue(user);
                if (userRoles is IEnumerable<string> stringRoles)
                {
                    roles.AddRange(stringRoles);
                }
                else if (userRoles is string singleRole)
                {
                    roles.Add(singleRole);
                }
                else if (userRoles != null)
                {
                    roles.Add(userRoles.ToString()!);
                }
            }
        }

        // If still no roles found, provide a default role
        if (!roles.Any())
        {
            roles.Add("User");
        }

        return roles;
    }
}
