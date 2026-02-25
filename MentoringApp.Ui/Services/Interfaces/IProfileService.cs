using MentoringApp.Api.DTOs.Profiles;

namespace MentoringApp.Ui.Services.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileDto?> GetProfileByIdAsync(int profileId);
        Task<ProfileDto?> GetMyProfileAsync(bool throwOnError);
        Task UpdateProfileAsync(UpdateProfileDto dto);
    }
}
