using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace WebApplication_JWT
{
    public class TokenManager
    {
        private static readonly string Secret = "QSMKDI#%^SJDmaisu&%&%dhsuhs@#$%^d/dsdfsvs@#$cs%$%^gdkjasb265487s**765dfsdf46s$^^$^5d4f8sdaeruwEO^$&&$RYOWHLKJXNCK$$^$^VSUKDFOsjfkdfk@#$%&&*!!!f";
        private static readonly int TokenExpirationInDays = 5;
        private static readonly HashSet<string> BlacklistedTokens = new HashSet<string>();

        public static string GenerateToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddDays(TokenExpirationInDays);

            var claims = new[] {
                new Claim(ClaimTypes.Name, username)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal GetPrincipal(string token)
        {
            try
            {
                if (IsTokenBlacklisted(token))
                    return null;

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(Secret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    RequireExpirationTime = true,
                    ValidateLifetime = true
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken && jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static string ValidateToken(string token)
        {
            var principal = GetPrincipal(token);
            if (principal == null)
                return null;

            var identity = principal.Identity as ClaimsIdentity;
            if (identity == null)
                return null;

            var usernameClaim = identity.FindFirst(ClaimTypes.Name);
            if (usernameClaim == null)
                return null;

            return usernameClaim.Value;
        }

        public static void AddToBlacklist(string token) => BlacklistedTokens.Add(token);

        public static bool IsTokenBlacklisted(string token)
        {
            return BlacklistedTokens.Contains(token);
        }
    }
}
