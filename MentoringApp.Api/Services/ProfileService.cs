using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Profiles;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using Microsoft.EntityFrameworkCore;

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

        public async Task<Profile?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Profile ID must be a positive integer.", nameof(id));
            
            Profile? profile = await _db.Profiles
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
            {
                throw new ArgumentException("Profile not found for ID: ", id.ToString());
            }

            return profile;
        }

        public async Task<Profile> CreateAsync(Profile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile), "Profile cannot be null.");

            _db.Profiles.Add(profile);
            await _db.SaveChangesAsync();

            return profile;

        }

        public async Task UpdateProfile(string UserId, UpdateProfileDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Updated profile cannot be null.");

            if (UserId == null)
                throw new ArgumentException("Profile must have a valid UserId.", nameof(dto));

            var existingProfile = _db.Profiles.FirstOrDefault(p => p.UserId == UserId);

            if (existingProfile == null)
            {
                throw new ArgumentException("Profile not found for UserId: ", UserId);
            }

            existingProfile.UserId = UserId;
            existingProfile.FirstName = dto.FirstName;
            existingProfile.LastName = dto.LastName;
            existingProfile.Bio = dto.Bio;

            _db.Profiles.Update(existingProfile);
            
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

    
