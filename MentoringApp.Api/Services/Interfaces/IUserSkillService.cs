using MentoringApp.Api.DTOs.UserSkills;

namespace MentoringApp.Api.Services.Interfaces
{
	public interface IUserSkillService
	{
		Task AddUserSkillAsync(string userId, AddUserSkillDto dto);
		Task UpdateUserSkillAsync(string userId, UpdateUserSkillDto dto);
		Task RemoveUserSkillAsync(string userId, int skillId);
		Task<List<UserSkillDto>> GetUserSkillsAsync(string userId);
		Task<UserSkillDto?> GetUserSkillAsync(string userId, int skillId);
		Task<List<UserSkillDto>> GetUsersBySkillAsync(int skillId);
	}
}
