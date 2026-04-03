using MentoringApp.Api.Models;
using MentoringApp.Api.DTOs.Skills;

namespace MentoringApp.Api.Services.Interfaces
{
	public interface ISkillService
	{
		Task<Skill> CreateSkillAsync(string userId, SkillCreateDto dto);
		Task<int> GetPendingSkillsCountAsync(string userId);
		Task<Skill?> GetSkillByNameAsync(string name);
		Task<Skill?> GetSkillByIdAsync(int id);
		Task<List<Skill>> GetAllSkillsAsync(bool includeApprovedOnly = false);
		Task ApproveSkillAsync(int skillId);
		Task ApproveSkillWithEditsAsync(int skillId, SkillModerationDto dto);
		Task RejectSkillAsync(int skillId);
		Task MarkSkillAsDuplicateAsync(int skillId, int existingSkillId);
		Task<List<Tag>> GetOrCreateTagsAsync(List<string> tagNames);
		Task<List<SkillCategory>> GetCategoriesAsync();
	}
}
