using System.IdentityModel.Tokens.Jwt;

namespace Excursion_GPT.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string login, string role);
    JwtSecurityToken ValidateToken(string token);
}