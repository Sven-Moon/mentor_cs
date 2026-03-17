using MentoringApp.Api.DTOs.UserSkills;
using MentoringApp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentoringApp.Api.Controllers
{
	[ApiController]
	[Route("api/users/me/skills")]
	[Authorize]
	public class UserSkillsController : ControllerBase
	{
		private readonly IUserSkillService _userSkillService;
		private readonly ILogger<UserSkillsController> _logger;

		public UserSkillsController(IUserSkillService userSkillService, ILogger<UserSkillsController> logger)
		{
			_userSkillService = userSkillService;
			_logger = logger;
		}

		private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

		/// <summary>
		/// GET /api/users/me/skills
		/// Returns all skills for the authenticated user.
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<List<UserSkillDto>>> GetMySkills(
			CancellationToken cancellationToken = default)
		{
			if (UserId == null)
				return Unauthorized();

			var skills = await _userSkillService.GetUserSkillsAsync(UserId);
			return Ok(skills);
		}

		/// <summary>
		/// GET /api/users/me/skills/{skillId}
		/// Returns a single user skill by skill ID.
		/// </summary>
		[HttpGet("{skillId:int}")]
		public async Task<ActionResult<UserSkillDto>> GetMySkill(
			int skillId,
			CancellationToken cancellationToken = default)
		{
			if (UserId == null)
				return Unauthorized();

			var userSkill = await _userSkillService.GetUserSkillAsync(UserId, skillId);

			if (userSkill == null)
				return NotFound();

			return Ok(userSkill);
		}

		/// <summary>
		/// POST /api/users/me/skills
		/// Add a skill to the authenticated user's profile.
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<UserSkillDto>> AddMySkill(
			AddUserSkillDto dto,
			CancellationToken cancellationToken = default)
		{
			if (UserId == null)
				return Unauthorized();

			try
			{
				await _userSkillService.AddUserSkillAsync(UserId, dto);

				var created = await _userSkillService.GetUserSkillAsync(UserId, dto.SkillId);
				return CreatedAtAction(
					nameof(GetMySkill),
					new { skillId = dto.SkillId },
					created);
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Failed to add skill {SkillId} for user {UserId}", dto.SkillId, UserId);
				return BadRequest(new { error = ex.Message });
			}
		}

		/// <summary>
		/// PUT /api/users/me/skills/{skillId}
		/// Update level or years of experience for an existing user skill.
		/// </summary>
		[HttpPut("{skillId:int}")]
		public async Task<IActionResult> UpdateMySkill(
			int skillId,
			UpdateUserSkillDto dto,
			CancellationToken cancellationToken = default)
		{
			if (UserId == null)
				return Unauthorized();

			// Ensure the route skillId matches the DTO
			dto.SkillId = skillId;

			try
			{
				await _userSkillService.UpdateUserSkillAsync(UserId, dto);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Failed to update skill {SkillId} for user {UserId}", skillId, UserId);
				return NotFound(new { error = ex.Message });
			}
		}

		/// <summary>
		/// DELETE /api/users/me/skills/{skillId}
		/// Remove a skill from the authenticated user's profile.
		/// </summary>
		[HttpDelete("{skillId:int}")]
		public async Task<IActionResult> RemoveMySkill(
			int skillId,
			CancellationToken cancellationToken = default)
		{
			if (UserId == null)
				return Unauthorized();

			try
			{
				await _userSkillService.RemoveUserSkillAsync(UserId, skillId);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Failed to remove skill {SkillId} for user {UserId}", skillId, UserId);
				return NotFound(new { error = ex.Message });
			}
		}
	}
}
