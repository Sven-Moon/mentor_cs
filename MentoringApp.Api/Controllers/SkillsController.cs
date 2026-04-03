using MentoringApp.Api.DTOs.Skills;
using MentoringApp.Api.Models;
using MentoringApp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentoringApp.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class SkillsController : ControllerBase
	{
		private readonly ISkillService _skillService;
		private readonly ILogger<SkillsController> _logger;

		public SkillsController(ISkillService skillService, ILogger<SkillsController> logger)
		{
			_skillService = skillService;
			_logger = logger;
		}

		/// <summary>
		/// GET /api/skills
		/// Returns all skills. Optionally filter to approved-only.
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<List<SkillResponseDto>>> GetAllSkills(
			[FromQuery] bool approvedOnly = false,
			CancellationToken cancellationToken = default)
		{
			var skills = await _skillService.GetAllSkillsAsync(approvedOnly);
			return Ok(skills.Select(MapToResponse).ToList());
		}

		/// <summary>
		/// GET /api/skills/search?query=
		/// Search approved skills by name (case-insensitive).
		/// </summary>
		[HttpGet("search")]
		public async Task<ActionResult<List<SkillResponseDto>>> SearchSkills(
			[FromQuery] string? query,
			CancellationToken cancellationToken = default)
		{
			var skills = await _skillService.GetAllSkillsAsync(includeApprovedOnly: true);

			if (!string.IsNullOrWhiteSpace(query))
			{
				var normalizedQuery = query.Trim().ToUpperInvariant();
				skills = skills
					.Where(s => s.NormalizedName.Contains(normalizedQuery))
					.ToList();
			}

			return Ok(skills.Select(MapToResponse).ToList());
		}

		/// <summary>
		/// GET /api/skills/{id}
		/// Returns a single skill by ID.
		/// </summary>
		[HttpGet("{id:int}")]
		public async Task<ActionResult<SkillResponseDto>> GetSkillById(
			int id,
			CancellationToken cancellationToken = default)
		{
			var skill = await _skillService.GetSkillByIdAsync(id);

			if (skill == null)
				return NotFound();

			return Ok(MapToResponse(skill));
		}

		/// <summary>
		/// GET /api/skills/categories
		/// Returns all skill categories.
		/// </summary>
		[HttpGet("categories")]
		public async Task<ActionResult<List<SkillCategoryDto>>> GetCategories(
			CancellationToken cancellationToken = default)
		{
			var categories = await _skillService.GetCategoriesAsync();
			return Ok(categories.Select(c => new SkillCategoryDto
			{
				Id = c.Id,
				Name = c.Name,
				Description = c.Description
			}).ToList());
		}

		/// <summary>
		/// POST /api/skills
		/// Creates a new skill (Pending status). Requires authentication.
		/// </summary>
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<SkillResponseDto>> CreateSkill(
			SkillCreateDto dto,
			CancellationToken cancellationToken = default)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
				return Unauthorized();

			try
			{
				var skill = await _skillService.CreateSkillAsync(userId, dto);
				return CreatedAtAction(
					nameof(GetSkillById),
					new { id = skill.Id },
					MapToResponse(skill));
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Skill creation failed for user {UserId}", userId);
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
