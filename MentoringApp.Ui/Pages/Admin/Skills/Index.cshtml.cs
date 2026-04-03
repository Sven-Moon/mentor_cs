using MentoringApp.Api.DTOs.Skills;
using MentoringApp.Ui.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MentoringApp.Ui.Pages.Admin.Skills;

[Authorize]
public class IndexModel : PageModel
{
		private readonly ApiClient _apiClient;

		public IndexModel(ApiClient apiClient)
		{
				_apiClient = apiClient;
		}

		public List<SkillResponseDto> PendingSkills { get; set; } = new();
		public List<SkillResponseDto> ApprovedSkills { get; set; } = new();
		public List<SkillCategoryDto> Categories { get; set; } = new();
		public string? SuccessMessage { get; set; }
		public string? ErrorMessage { get; set; }

		// Edit & Approve form
		[BindProperty] public int EditSkillId { get; set; }
		[BindProperty] public string EditName { get; set; } = "";
		[BindProperty] public string EditDescription { get; set; } = "";
		[BindProperty] public string EditTags { get; set; } = "";
		[BindProperty] public List<int> EditCategoryIds { get; set; } = new();

		// Mark Duplicate form
		[BindProperty] public int DuplicateSkillId { get; set; }
		[BindProperty] public int ExistingSkillId { get; set; }

		public async Task OnGetAsync()
		{
				await LoadDataAsync();
		}

		public async Task<IActionResult> OnPostApproveSkillAsync(int skillId)
		{
				try
				{
						await _apiClient.ApproveSkillAsync(skillId);
						SuccessMessage = "Skill approved.";
				}
				catch (ApiException ex)
				{
						ErrorMessage = ex.StatusCode == 403
								? "You do not have permission to perform this action."
								: ex.Message;
				}

				await LoadDataAsync();
				return Page();
		}

		public async Task<IActionResult> OnPostApproveWithEditsAsync()
		{
				try
				{
						var tags = EditTags
								.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
								.ToList(); // doesn't handle null

						await _apiClient.ApproveSkillWithEditsAsync(EditSkillId, new SkillModerationDto
						{
								Name = EditName,
								Description = EditDescription,
								Tags = tags,
								CategoryIds = EditCategoryIds
						});
						SuccessMessage = "Skill approved with edits.";
				}
				catch (ApiException ex)
				{
						ErrorMessage = ex.StatusCode == 403
								? "You do not have permission to perform this action."
								: ex.Message;
				}

				await LoadDataAsync();
				return Page();
		}

		public async Task<IActionResult> OnPostRejectSkillAsync(int skillId)
		{
				try
				{
						await _apiClient.RejectSkillAsync(skillId);
						SuccessMessage = "Skill rejected and removed.";
				}
				catch (ApiException ex)
				{
						ErrorMessage = ex.StatusCode == 403
								? "You do not have permission to perform this action."
								: ex.Message;
				}

				await LoadDataAsync();
				return Page();
		}

		public async Task<IActionResult> OnPostMarkDuplicateAsync()
		{
				try
				{
						await _apiClient.MarkSkillAsDuplicateAsync(DuplicateSkillId, ExistingSkillId);
						SuccessMessage = "Skill marked as duplicate and users reassigned.";
				}
				catch (ApiException ex)
				{
						ErrorMessage = ex.StatusCode == 403
								? "You do not have permission to perform this action."
								: ex.Message;
				}

				await LoadDataAsync();
				return Page();
		}

		private async Task LoadDataAsync()
		{
				try { PendingSkills = await _apiClient.GetPendingSkillsAsync(); } catch { }
				try { ApprovedSkills = await _apiClient.GetAllSkillsAsync(approvedOnly: true); } catch { }
				try { Categories = await _apiClient.GetCategoriesAsync(); } catch { }
		}
}
