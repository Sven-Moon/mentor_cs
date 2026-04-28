using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Mentorship;
using MentoringApp.Api.DTOs.Skills;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MentoringApp.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class MentorshipsController : ControllerBase
	{
		private readonly ApplicationDbContext _db;
		private readonly UserManager<ApplicationUser> _userManager;

		public MentorshipsController(
			ApplicationDbContext context,
			UserManager<ApplicationUser> userManager)
		{
			_db = context;
			_userManager = userManager;
		}

		private string UserId =>
			User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		private bool IsAdmin =>
			User.IsInRole("Admin");

		// ----------------------------------------------------------------
		// MENTORSHIP CRUD
		// ----------------------------------------------------------------

		/// <summary>Get mentorships for the current user (mentor or mentee). Admins get all.</summary>
		[HttpGet]
		public async Task<ActionResult<IEnumerable<MentorshipDto>>> GetMentorships()
		{
			IQueryable<Mentorship> query = _db.Mentorships;

			if (!IsAdmin)
				query = query.Where(m => m.MentorId == UserId || m.MenteeId == UserId);

			var result = await query.Select(m => new
			{
				m.Id,
				m.MentorId,
				m.MenteeId,
				Mentor = new { m.Mentor.Id, m.Mentor.UserName },
				Mentee = new { m.Mentee.Id, m.Mentee.UserName },
				m.Scope,
				m.Status,
				m.StartDate,
				m.EndDate,
				m.LastInteractionDate
			}).ToListAsync();

			return Ok(result);
		}

		/// <summary>Get a single mentorship by ID.</summary>
		[HttpGet("{id:int}")]
		public async Task<ActionResult<MentorshipDto>> GetMentorship(int id)
		{
			var result = await _db.Mentorships.Where(m => m.Id == id).Select(m => new
			{
				m.Id,
				m.MentorId,
				m.MenteeId,
				Mentor = new { m.Mentor.Id, m.Mentor.UserName },
				Mentee = new { m.Mentee.Id, m.Mentee.UserName },
				m.Scope,
				m.Status,
				m.StartDate,
				m.EndDate,
				m.LastInteractionDate
			}).FirstOrDefaultAsync();

			if (result == null)
				return NotFound();

			if (!IsAdmin && result.MentorId != UserId && result.MenteeId != UserId)
				return Forbid();

			return Ok(result);
		}

		/// <summary>Create a mentorship (admin only).</summary>
		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<MentorshipDto>> CreateMentorship([FromBody] MentorshipDto dto)
		{
			var entity = new Mentorship
			{
				MentorId = dto.MentorId,
				MenteeId = dto.MenteeId,
				Scope = dto.Scope,
				Status = dto.Status,
				StartDate = ToUtc(dto.StartDate),
				EndDate = ToUtc(dto.EndDate),
				LastInteractionDate = ToUtc(dto.LastInteractionDate)
			};

			_db.Mentorships.Add(entity);
			await _db.SaveChangesAsync();

			dto.Id = entity.Id;
			return CreatedAtAction(nameof(GetMentorship), new { id = entity.Id }, dto);
		}

		/// <summary>Update a mentorship. Mentors and mentees involved may update it.</summary>
		[HttpPut("{id:int}")]
		public async Task<IActionResult> UpdateMentorship(int id, [FromBody] MentorshipDto updated)
		{
			if (id != updated.Id)
				return BadRequest();

			var mentorship = await _db.Mentorships.FirstOrDefaultAsync(m => m.Id == id);
			if (mentorship == null)
				return NotFound();

			if (!IsAdmin && mentorship.MentorId != UserId && mentorship.MenteeId != UserId)
				return Forbid();

			mentorship.Scope = updated.Scope;
			mentorship.StartDate = ToUtc(updated.StartDate);
			mentorship.EndDate = ToUtc(updated.EndDate);
			mentorship.Status = updated.Status;
			mentorship.LastInteractionDate = ToUtc(updated.LastInteractionDate);

			if (_db.Database.IsNpgsql())
			{
				_db.Entry(mentorship)
					.Property("xmin")
					.OriginalValue = BitConverter.ToUInt32(updated.Version);
			}

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				return Conflict("This mentorship was modified by another user.");
			}

			return NoContent();
		}

		/// <summary>Delete a mentorship (admin only).</summary>
		[HttpDelete("{id:int}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeleteMentorship(int id)
		{
			var mentorship = await _db.Mentorships.FindAsync(id);
			if (mentorship == null)
				return NotFound();

			_db.Mentorships.Remove(mentorship);
			await _db.SaveChangesAsync();
			return NoContent();
		}

		// ----------------------------------------------------------------
		// GOALS
		// ----------------------------------------------------------------

		[HttpGet("{id:int}/goals")]
		public async Task<ActionResult<IEnumerable<GoalDto>>> GetGoals(int id)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var goals = await _db.Goals
				.Where(g => g.MentorshipId == id)
				.ToListAsync();

			return Ok(goals.Select(ToDto));
		}

		[HttpPost("{id:int}/goals")]
		public async Task<ActionResult<GoalDto>> CreateGoal(int id, [FromBody] CreateGoalDto dto)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var goal = new Goal
			{
				MentorshipId = id,
				Title = dto.Title,
				Description = dto.Description,
				DueDate = dto.DueDate,
				CreatedAt = DateTime.UtcNow,
				Status = GoalStatus.Pending
			};

			_db.Goals.Add(goal);
			await _db.SaveChangesAsync();

			return CreatedAtAction(nameof(GetGoals), new { id }, ToDto(goal));
		}

		[HttpPut("{id:int}/goals/{goalId:int}")]
		public async Task<IActionResult> UpdateGoal(int id, int goalId, [FromBody] UpdateGoalDto dto)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var goal = await _db.Goals
				.FirstOrDefaultAsync(g => g.Id == goalId && g.MentorshipId == id);
			if (goal == null)
				return NotFound();

			goal.Title = dto.Title;
			goal.Description = dto.Description;
			goal.DueDate = dto.DueDate;
			goal.CompletedAt = dto.CompletedAt;

			if (dto.Status.HasValue)
				goal.Status = dto.Status.Value;

			await _db.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("{id:int}/goals/{goalId:int}")]
		public async Task<IActionResult> DeleteGoal(int id, int goalId)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var goal = await _db.Goals
				.FirstOrDefaultAsync(g => g.Id == goalId && g.MentorshipId == id);
			if (goal == null)
				return NotFound();

			_db.Goals.Remove(goal);
			await _db.SaveChangesAsync();
			return NoContent();
		}

		// ----------------------------------------------------------------
		// MILESTONES
		// ----------------------------------------------------------------

		[HttpGet("{id:int}/milestones")]
		public async Task<ActionResult<IEnumerable<MilestoneDto>>> GetMilestones(int id)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var milestones = await _db.Milestones
				.Where(ms => ms.MentorshipId == id)
				.ToListAsync();

			return Ok(milestones.Select(ToDto));
		}

		[HttpPost("{id:int}/milestones")]
		public async Task<ActionResult<MilestoneDto>> CreateMilestone(int id, [FromBody] CreateMilestoneDto dto)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var milestone = new Milestone
			{
				MentorshipId = id,
				Title = dto.Title,
				Description = dto.Description,
				DueDate = dto.DueDate,
				CreatedAt = DateTime.UtcNow
			};

			_db.Milestones.Add(milestone);
			await _db.SaveChangesAsync();

			return CreatedAtAction(nameof(GetMilestones), new { id }, ToDto(milestone));
		}

		[HttpPut("{id:int}/milestones/{milestoneId:int}")]
		public async Task<IActionResult> UpdateMilestone(int id, int milestoneId, [FromBody] UpdateMilestoneDto dto)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var milestone = await _db.Milestones
				.FirstOrDefaultAsync(ms => ms.Id == milestoneId && ms.MentorshipId == id);
			if (milestone == null)
				return NotFound();

			milestone.Title = dto.Title;
			milestone.Description = dto.Description;
			milestone.DueDate = dto.DueDate;
			milestone.CompletedAt = dto.CompletedAt;

			await _db.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("{id:int}/milestones/{milestoneId:int}")]
		public async Task<IActionResult> DeleteMilestone(int id, int milestoneId)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var milestone = await _db.Milestones
				.FirstOrDefaultAsync(ms => ms.Id == milestoneId && ms.MentorshipId == id);
			if (milestone == null)
				return NotFound();

			_db.Milestones.Remove(milestone);
			await _db.SaveChangesAsync();
			return NoContent();
		}

		// ----------------------------------------------------------------
		// SESSIONS
		// ----------------------------------------------------------------

		[HttpGet("{id:int}/sessions")]
		public async Task<ActionResult<IEnumerable<SessionDto>>> GetSessions(int id)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var sessions = await _db.Sessions
				.Where(s => s.MentorshipId == id)
				.ToListAsync();

			return Ok(sessions.Select(ToDto));
		}

		[HttpPost("{id:int}/sessions")]
		public async Task<ActionResult<SessionDto>> CreateSession(int id, [FromBody] CreateSessionDto dto)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var session = new Session
			{
				MentorshipId = id,
				Title = dto.Title,
				ScheduledAt = dto.ScheduledAt,
				Duration = dto.Duration,
				Notes = dto.Notes,
				CreatedAt = DateTime.UtcNow
			};

			_db.Sessions.Add(session);
			await _db.SaveChangesAsync();

			return CreatedAtAction(nameof(GetSessions), new { id }, ToDto(session));
		}

		[HttpPut("{id:int}/sessions/{sessionId:int}")]
		public async Task<IActionResult> UpdateSession(int id, int sessionId, [FromBody] UpdateSessionDto dto)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var session = await _db.Sessions
				.FirstOrDefaultAsync(s => s.Id == sessionId && s.MentorshipId == id);
			if (session == null)
				return NotFound();

			session.Title = dto.Title;
			session.ScheduledAt = dto.ScheduledAt;
			session.Duration = dto.Duration;
			session.Notes = dto.Notes;

			await _db.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("{id:int}/sessions/{sessionId:int}")]
		public async Task<IActionResult> DeleteSession(int id, int sessionId)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var session = await _db.Sessions
				.FirstOrDefaultAsync(s => s.Id == sessionId && s.MentorshipId == id);
			if (session == null)
				return NotFound();

			_db.Sessions.Remove(session);
			await _db.SaveChangesAsync();
			return NoContent();
		}

		// ----------------------------------------------------------------
		// NOTES
		// ----------------------------------------------------------------

		[HttpGet("{id:int}/notes")]
		public async Task<ActionResult<IEnumerable<NoteDto>>> GetNotes(int id)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var notes = await _db.Notes
				.Where(n => n.MentorshipId == id)
				.ToListAsync();

			return Ok(notes.Select(ToDto));
		}

		[HttpPost("{id:int}/notes")]
		public async Task<ActionResult<NoteDto>> CreateNote(int id, [FromBody] CreateNoteDto dto)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var note = new Note
			{
				MentorshipId = id,
				AuthorId = UserId,
				Content = dto.Content,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_db.Notes.Add(note);
			await _db.SaveChangesAsync();

			return CreatedAtAction(nameof(GetNotes), new { id }, ToDto(note));
		}

		[HttpPut("{id:int}/notes/{noteId:int}")]
		public async Task<IActionResult> UpdateNote(int id, int noteId, [FromBody] UpdateNoteDto dto)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var note = await _db.Notes
				.FirstOrDefaultAsync(n => n.Id == noteId && n.MentorshipId == id);
			if (note == null)
				return NotFound();

			if (!IsAdmin && note.AuthorId != UserId)
				return Forbid();

			note.Content = dto.Content;
			note.UpdatedAt = DateTime.UtcNow;

			await _db.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("{id:int}/notes/{noteId:int}")]
		public async Task<IActionResult> DeleteNote(int id, int noteId)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var note = await _db.Notes
				.FirstOrDefaultAsync(n => n.Id == noteId && n.MentorshipId == id);
			if (note == null)
				return NotFound();

			if (!IsAdmin && note.AuthorId != UserId)
				return Forbid();

			_db.Notes.Remove(note);
			await _db.SaveChangesAsync();
			return NoContent();
		}

		// ----------------------------------------------------------------
		// SKILLS (many-to-many)
		// ----------------------------------------------------------------

		[HttpGet("{id:int}/skills")]
		public async Task<ActionResult<IEnumerable<SkillDto>>> GetSkills(int id)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var mentorship = await _db.Mentorships
				.Include(m => m.Skills)
				.FirstAsync(m => m.Id == id);

			return Ok(mentorship.Skills.Select(s => new SkillDto
			{
				Id = s.Id,
				Name = s.Name,
				Description = s.Description
			}));
		}

		[HttpPost("{id:int}/skills/{skillId:int}")]
		public async Task<IActionResult> AddSkill(int id, int skillId)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var mentorship = await _db.Mentorships
				.Include(m => m.Skills)
				.FirstAsync(m => m.Id == id);

			if (mentorship.Skills.Any(s => s.Id == skillId))
				return NoContent();

			var skill = await _db.Skills.FindAsync(skillId);
			if (skill == null)
				return NotFound();

			mentorship.Skills.Add(skill);
			await _db.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("{id:int}/skills/{skillId:int}")]
		public async Task<IActionResult> RemoveSkill(int id, int skillId)
		{
			var guard = await AuthorizeMentorshipAsync(id);
			if (guard != null) return guard;

			var mentorship = await _db.Mentorships
				.Include(m => m.Skills)
				.FirstAsync(m => m.Id == id);

			var skill = mentorship.Skills.FirstOrDefault(s => s.Id == skillId);
			if (skill == null)
				return NotFound();

			mentorship.Skills.Remove(skill);
			await _db.SaveChangesAsync();
			return NoContent();
		}

		// ----------------------------------------------------------------
		// Private helpers
		// ----------------------------------------------------------------

		/// <summary>
		/// Returns null if the current user may access the mentorship.
		/// Returns NotFound or Forbid as an ActionResult if not.
		/// </summary>
		private async Task<ActionResult?> AuthorizeMentorshipAsync(int mentorshipId)
		{
			var mentorship = await _db.Mentorships.FindAsync(mentorshipId);
			if (mentorship == null)
				return NotFound();
			if (!IsAdmin && mentorship.MentorId != UserId && mentorship.MenteeId != UserId)
				return Forbid();
			return null;
		}

		private static DateTime ToUtc(DateTime dt) =>
			dt.Kind == DateTimeKind.Unspecified
				? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
				: dt.ToUniversalTime();

		private static DateTime? ToUtc(DateTime? dt) =>
			dt.HasValue ? ToUtc(dt.Value) : null;

		private static GoalDto ToDto(Goal g) => new()
		{
			Id = g.Id,
			MentorshipId = g.MentorshipId,
			Title = g.Title,
			Description = g.Description,
			Status = g.Status.ToString(),
			DueDate = g.DueDate,
			CreatedAt = g.CreatedAt,
			CompletedAt = g.CompletedAt
		};

		private static MilestoneDto ToDto(Milestone ms) => new()
		{
			Id = ms.Id,
			MentorshipId = ms.MentorshipId,
			Title = ms.Title,
			Description = ms.Description,
			DueDate = ms.DueDate,
			CreatedAt = ms.CreatedAt,
			CompletedAt = ms.CompletedAt
		};

		private static SessionDto ToDto(Session s) => new()
		{
			Id = s.Id,
			MentorshipId = s.MentorshipId,
			Title = s.Title,
			ScheduledAt = s.ScheduledAt,
			Duration = s.Duration,
			Notes = s.Notes,
			CreatedAt = s.CreatedAt
		};

		private static NoteDto ToDto(Note n) => new()
		{
			Id = n.Id,
			MentorshipId = n.MentorshipId,
			AuthorId = n.AuthorId,
			Content = n.Content,
			CreatedAt = n.CreatedAt,
			UpdatedAt = n.UpdatedAt
		};
	}
}
