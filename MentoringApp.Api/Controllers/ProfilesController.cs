using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Profiles;
using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/profile/me
        [HttpGet("me")]
        public async Task<ActionResult<ProfileDto>> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var profile = await _context.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return NotFound();

            return Ok(ToDto(profile));
        }

        // GET: api/profile/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProfileDto>> GetProfile(int id)
        {
            var profile = await _context.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
                return NotFound();

            return Ok(ToDto(profile));
        }

        // POST: api/profile
        [HttpPost]
        public async Task<ActionResult<ProfileDto>> CreateProfile(CreateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingProfile = await _context.Profiles
                .AnyAsync(p => p.UserId == userId);

            if (existingProfile)
                return Conflict("Profile already exists for this user");

            var profile = new Profile
            {
                UserId = userId!,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Bio = dto.Bio,
                Location = dto.Location,
                CreatedAt = DateTime.UtcNow
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetProfile),
                new { id = profile.Id },
                ToDto(profile));
        }

        // PUT: api/profile/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return NotFound();

            profile.FirstName = dto.FirstName;
            profile.LastName = dto.LastName;
            profile.Bio = dto.Bio;
            profile.Location = dto.Location;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static ProfileDto ToDto(Profile profile)
        {
            return new ProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Bio = profile.Bio,
                Location = profile.Location
            };
        }
    }
}