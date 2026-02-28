using MentoringApp.Api.DTOs.Profiles;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;

namespace MentoringApp.Api.Services
{
    public interface IProfileService
    {
        Task<Profile?> GetByUserIdAsync(string userId);
        Task<Profile?> GetByIdAsync(int id);
        Task<Profile> CreateDefaultProfileAsync(ApplicationUser user);
        Task<Profile> CreateAsync(Profile profile);
        Task UpdateProfile(string UserId, UpdateProfileDto profile);
        Task DeleteByUserIdAsync(string userId);
    }
}
