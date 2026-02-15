using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Auth;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MentoringApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto dto)
        {
            var existing = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existing != null)
                return BadRequest("Email already in use.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                IsActive = true // Default to active
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // await _userManager.AddToRoleAsync(user, "User");

            return Ok(new { message = "Registration successful." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null) return Unauthorized("Invalid credentials");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

            if (!result.Succeeded) return Unauthorized("Invalid credentials");

            var accessToken = GenerateJwtToken(user);

            // generate and persist refresh token
            var refreshToken = GenerateRefreshToken();
            var refresh = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                Revoked = false
            };

            _db.RefreshTokens.Add(refresh);
            await _db.SaveChangesAsync();

            // Set refresh token as Secure HttpOnly cookie. For cross-origin frontends, front-end must call with credentials: 'include'
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = refresh.ExpiresAt
            });

            return Ok(new AuthResponseDto
            {
                Token = accessToken,
                UserId = user.Id,
                Email = user.Email!
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var incomingToken) || string.IsNullOrEmpty(incomingToken))
                return Unauthorized("No refresh token");

            var existing = await _db.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Token == incomingToken && !rt.Revoked);

            if (existing == null || existing.ExpiresAt < DateTime.UtcNow)
                return Unauthorized("Invalid refresh token");

            var user = await _userManager.FindByIdAsync(existing.UserId);
            if (user == null) return Unauthorized("Invalid user");

            // rotate refresh token: revoke current and create new
            var toRevoke = await _db.RefreshTokens.FindAsync(existing.Id);
            if (toRevoke != null)
            {
                toRevoke.Revoked = true;
                toRevoke.RevokedAt = DateTime.UtcNow;
            }

            var newRefreshToken = GenerateRefreshToken();
            var newRefresh = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                Revoked = false
            };

            _db.RefreshTokens.Add(newRefresh);
            await _db.SaveChangesAsync();

            // set new cookie
            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = newRefresh.ExpiresAt
            });

            var newAccess = GenerateJwtToken(user);

            return Ok(new AuthResponseDto
            {
                Token = newAccess,
                UserId = user.Id,
                Email = user.Email!
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var incomingToken) && !string.IsNullOrEmpty(incomingToken))
            {
                var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == incomingToken && !rt.Revoked);
                if (existing != null)
                {
                    existing.Revoked = true;
                    existing.RevokedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }

            // remove cookie
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            return Ok();
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            // Ensure configuration values are present to avoid passing null to Encoding.GetBytes
            var keyValue = _config["Jwt:Key"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Key' is not set.");
            var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Issuer' is not set.");
            var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Audience' is not set.");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken(int size = 64)
        {
            var randomNumber = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

    } // end of AuthController class
}