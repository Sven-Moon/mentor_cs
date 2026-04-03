using MentoringApp.Api.DTOs.Skills;
using MentoringApp.Api.Enums;
using UserSkillDto = MentoringApp.Api.DTOs.UserSkills.UserSkillDto;
using AddUserSkillDto = MentoringApp.Api.DTOs.UserSkills.AddUserSkillDto;
using UpdateUserSkillDto = MentoringApp.Api.DTOs.UserSkills.UpdateUserSkillDto;
using MentoringApp.Ui.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MentoringApp.Ui.Pages.Skills;

[Authorize]
public class IndexModel : PageModel
{
		private readonly ApiClient _apiClient;

		public IndexModel(ApiClient apiClient)
		{
				_apiClient = apiClient;
		}

		// Display data
		public List<UserSkillDto> MySkills { get; set; } = new();
		public List<SkillResponseDto> ApprovedSkills { get; set; } = new();
		public List<SkillCategoryDto> Categories { get; set; } = new();
		public string? SuccessMessage { get; set; }
		public string? ErrorMessage { get; set; }
		public string ActiveTab { get; set; } = "mySkills";

		// Add skill form
		[BindProperty] public int AddSkillId { get; set; }
		[BindProperty] public SkillLevel AddLevel { get; set; } = SkillLevel.Beginner;
		[BindProperty] public int? AddYearsExperience { get; set; }

		// Update skill form
		[BindProperty] public int UpdateSkillId { get; set; }
		[BindProperty] public SkillLevel UpdateLevel { get; set; } = SkillLevel.Beginner;
		[BindProperty] public int? UpdateYearsExperience { get; set; }

		// Propose new skill form
		[BindProperty] public string ProposeName { get; set; } = "";
		[BindProperty] public string ProposeDescription { get; set; } = "";
		[BindProperty] public string ProposeTags { get; set; } = "";
		[BindProperty] public List<int> ProposeCategoryIds { get; set; } = new();

		public async Task OnGetAsync()
		{
				await LoadPageDataAsync();
		}

		public async Task<IActionResult> OnPostAddSkillAsync()
		{
				try
				{
						await _apiClient.AddUserSkillAsync(new AddUserSkillDto
						{
								SkillId = AddSkillId,
								Level = AddLevel,
								YearsExperience = AddYearsExperience
						});
						SuccessMessage = "Skill added to your profile.";
						ActiveTab = "mySkills";
				}
				catch (ApiException ex)
				{
						ErrorMessage = ex.Message;
						ActiveTab = "browseSkills";
				}

				await LoadPageDataAsync();
				return Page();
		}

		public async Task<IActionResult> OnPostUpdateSkillAsync()
		{
				try
				{
						await _apiClient.UpdateUserSkillAsync(UpdateSkillId, new UpdateUserSkillDto
						{
								SkillId = UpdateSkillId,
								Level = UpdateLevel,
								YearsExperience = UpdateYearsExperience
						});
						SuccessMessage = "Skill updated.";
				}
				catch (ApiException ex)
				{
						ErrorMessage = ex.Message;
				}

				ActiveTab = "mySkills";
				await LoadPageDataAsync();
				return Page();
		}

		public async Task<IActionResult> OnPostRemoveSkillAsync(int skillId)
		{
				try
				{
						await _apiClient.RemoveUserSkillAsync(skillId);
						SuccessMessage = "Skill removed from your profile.";
				}
				catch (ApiException ex)
				{
						ErrorMessage = ex.Message;
				}

				ActiveTab = "mySkills";
				await LoadPageDataAsync();
				return Page();
		}

		public async Task<IActionResult> OnPostProposeSkillAsync()
		{
				if (string.IsNullOrWhiteSpace(ProposeName))
				{
						ErrorMessage = "Skill name is required.";
						ActiveTab = "proposeSkill";
						await LoadPageDataAsync();
						return Page();
				}

				try
				{
						var tags = (ProposeTags ?? "")
								.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
								.ToList();

						await _apiClient.CreateSkillAsync(new SkillCreateDto
						{
								Name = ProposeName,
								Description = ProposeDescription,
								Tags = tags,
								CategoryIds = ProposeCategoryIds
						});
						SuccessMessage = "Your skill proposal has been submitted for review.";
						ActiveTab = "mySkills";
				}
				catch (ApiException ex)
				{
						ErrorMessage = ex.Message;
						ActiveTab = "proposeSkill";
				}

				await LoadPageDataAsync();
				return Page();
		}

		private async Task LoadPageDataAsync()
		{
				try { MySkills = await _apiClient.GetMySkillsAsync(); } catch { }
				try { ApprovedSkills = await _apiClient.GetAllSkillsAsync(approvedOnly: true); } catch { }
				try { Categories = await _apiClient.GetCategoriesAsync(); } catch { }
		}
}
