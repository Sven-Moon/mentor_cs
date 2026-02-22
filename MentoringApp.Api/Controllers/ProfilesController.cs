using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Profiles;
using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MentoringApp.Api.Services;

namespace MentoringApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ProfileService _profileService;

        public ProfileController(ApplicationDbContext db, ProfileService profileService)
        {
            _db = db;
            _profileService = profileService;
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
            var profile = await _profileService.GetByIdAsync(id);

            if (profile == null)
                return NotFound();

            return Ok(ToDto(profile));
        }

        // POST: api/profile
        [HttpPost]
        public async Task<ActionResult<ProfileDto>> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User cannot be found");
            }

            await _profileService.UpdateProfile(userId, dto);

            // Load existing profile for the user
            Profile? profile = await _db.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return NotFound();
            }

            UpdateProfileDto updateDto = new UpdateProfileDto
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Bio = dto.Bio,
                Location = dto.Location
            };

            await _profileService.UpdateProfile(profile.UserId, updateDto);

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