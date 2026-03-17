using MentoringApp.Api.DTOs.Skills;
using MentoringApp.Api.Enums;
using MentoringApp.Api.Models;
using MentoringApp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoringApp.Api.Controllers
{
	[ApiController]
	[Route("api/admin/skills")]
	[Authorize(Roles = "Admin")]
	public class AdminSkillsController : ControllerBase
	{
		private readonly ISkillService _skillService;
		private readonly ILogger<AdminSkillsController> _logger;

		public AdminSkillsController(ISkillService skillService, ILogger<AdminSkillsController> logger)
		{
			_skillService = skillService;
			_logger = logger;
		}

		/// <summary>
		/// GET /api/admin/skills
		/// Returns all skills (all statuses).
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<List<SkillResponseDto>>> GetAllSkills(
			CancellationToken cancellationToken = default)
		{
			var skills = await _skillService.GetAllSkillsAsync(includeApprovedOnly: false);
			return Ok(skills.Select(MapToResponse).ToList());
		}

		/// <summary>
		/// GET /api/admin/skills/pending
		/// Returns only pending skills awaiting moderation.
		/// </summary>
		[HttpGet("pending")]
		public async Task<ActionResult<List<SkillResponseDto>>> GetPendingSkills(
			CancellationToken cancellationToken = default)
		{
			var skills = await _skillService.GetAllSkillsAsync(includeApprovedOnly: false);
			var pending = skills.Where(s => s.Status == SkillStatus.Pending).ToList();
			return Ok(pending.Select(MapToResponse).ToList());
		}

		/// <summary>
		/// POST /api/admin/skills/{id}/approve
		/// Approve a skill as-is.
		/// </summary>
		[HttpPost("{id:int}/approve")]
		public async Task<IActionResult> ApproveSkill(
			int id,
			CancellationToken cancellationToken = default)
		{
			try
			{
				await _skillService.ApproveSkillAsync(id);
				_logger.LogInformation("Skill {SkillId} approved", id);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

		/// <summary>
		/// PUT /api/admin/skills/{id}/approve
		/// Approve a skill with edits (name, description, tags).
		/// </summary>
		[HttpPut("{id:int}/approve")]
		public async Task<IActionResult> ApproveSkillWithEdits(
			int id,
			SkillModerationDto dto,
			CancellationToken cancellationToken = default)
		{
			try
			{
				await _skillService.ApproveSkillWithEditsAsync(id, dto);
				_logger.LogInformation("Skill {SkillId} approved with edits", id);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

		/// <summary>
		/// POST /api/admin/skills/{id}/reject
		/// Reject and remove a skill.
		/// </summary>
		[HttpPost("{id:int}/reject")]
		public async Task<IActionResult> RejectSkill(
			int id,
			CancellationToken cancellationToken = default)
		{
			try
			{
				await _skillService.RejectSkillAsync(id);
				_logger.LogInformation("Skill {SkillId} rejected", id);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				return NotFound(new { error = ex.Message });
			}
		}

		/// <summary>
		/// POST /api/admin/skills/{id}/duplicate
		/// Mark a skill as duplicate of an existing approved skill.
		/// </summary>
		[HttpPost("{id:int}/duplicate")]
		public async Task<IActionResult> MarkAsDuplicate(
			int id,
			MarkDuplicateDto dto,
			CancellationToken cancellationToken = default)
		{
			try
			{
				await _skillService.MarkSkillAsDuplicateAsync(id, dto.ExistingSkillId);
				_logger.LogInformation("Skill {SkillId} marked as duplicate of {ExistingSkillId}", id, dto.ExistingSkillId);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { error = ex.Message });
			}
		}

		private static SkillResponseDto MapToResponse(Skill skill)
		{
			return new SkillResponseDto
			{
				Id = skill.Id,
				Name = skill.Name,
				Description = skill.Description,
				Status = skill.Status,
				DuplicateOfSkillId = skill.DuplicateOfSkillId,
				Categories = skill.Categories.Select(c => c.Name).ToList(),
				Tags = skill.Tags.Select(t => t.Name).ToList()
			};
		}
	}
}
