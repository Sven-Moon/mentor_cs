using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Profiles;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MentoringApp.Api.DTOs.Common;

namespace MentoringApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ProfileController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/profile/me
        [HttpGet("me")]
        public async Task<ActionResult<ProfileDto>> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var profile = await _db.Profiles
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
            var profile = await _db.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
                return NotFound();

            return Ok(ToDto(profile));
        }

        // POST: api/profile
        [HttpPost]
        public async Task<ActionResult<ProfileDto>> UpsertProfile(UpsertProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Load existing profile for the user
            var profile = await _db.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                var newProfile = new Profile
                {
                    UserId = userId!,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Bio = dto.Bio,
                    Location = dto.Location,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Profiles.Add(newProfile);
                await _db.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetProfile),
                    new { id = newProfile.Id },
                    ToDto(newProfile));
            }

            // Update existing profile but ignore null values from dto
            if (dto.FirstName != null)
                profile.FirstName = dto.FirstName;

            if (dto.LastName != null)
                profile.LastName = dto.LastName;

            if (dto.Bio != null)
                profile.Bio = dto.Bio;

            if (dto.Location != null)
                profile.Location = dto.Location;

            await _db.SaveChangesAsync();

            return Ok(ToDto(profile));
        }

        // PUT: api/profile/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var profile = await _db.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return NotFound();

            profile.FirstName = dto.FirstName;
            profile.LastName = dto.LastName;
            profile.Bio = dto.Bio;
            profile.Location = dto.Location;

            await _db.SaveChangesAsync();

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