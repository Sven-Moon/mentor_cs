using MentoringApp.Api.Data;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using MentoringApp.Api.DTOs.Profiles;

namespace MentoringApp.Api.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _db;

        public ProfileService(ApplicationDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<Profile> CreateDefaultProfileAsync(ApplicationUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "User cannot be null.");

            var profile = new Profile
            {
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
            };

            _db.Profiles.Add(profile);
            await _db.SaveChangesAsync();

            return profile;
        }

        public async Task<Profile?> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _db.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<Profile> CreateAsync(Profile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile), "Profile cannot be null.");

            _db.Profiles.Add(profile);
            await _db.SaveChangesAsync();

            return profile;

        }

        public async Task UpdateProfile(UpdateProfileDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Updated profile cannot be null.");

            var userId = dto.UserId;

            if (userId == null)
                throw new ArgumentException("Profile must have a valid UserId.", nameof(dto));

            var existingProfile = _db.Profiles.FirstOrDefault(p => p.UserId == userId);

            if (existingProfile == null)
            {
                _db.Profiles.Add(profile);
            }

            _db.Profiles.Update(dto);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var existing = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (existing != null)
            {
                _db.Profiles.Remove(existing);
                await _db.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException($"No profile found for user ID '{userId}'.");
            }
        }
       
    }
}

    
