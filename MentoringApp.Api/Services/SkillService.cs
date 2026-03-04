using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Skills;
using MentoringApp.Api.Enums;
using MentoringApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Services
{
	public class SkillService
	{
		private readonly ApplicationDbContext _db;

		public SkillService(ApplicationDbContext db)
		{
			_db = db;
		}

		public async Task<Skill> CreateSkillAsync(string userId, SkillCreateDto dto)
		{
			// Check if a skill with the same name already exists (case-insensitive)
			var existingSkill = await _db.Skills
				.FirstOrDefaultAsync(s => s.Name.ToLower() == dto.Name.ToLower());

			if (existingSkill != null)
			{
				throw new InvalidOperationException("A skill with the same name already exists.");
			}

			// Check pending skills limit
			var pendingCount = await GetPendingSkillsCountAsync(userId);
			if (pendingCount >= 10)
			{
				throw new InvalidOperationException("You have reached the limit of 10 pending skills. Please wait for them to be reviewed before adding more.");
			}

			// Create new skill
			var skill = new Skill
			{
				Name = dto.Name,
				Description = dto.Description,
				Status = SkillStatus.Pending
			};

			_db.Skills.Add(skill);
			await _db.SaveChangesAsync();

			// Add tags
			if (dto.Tags.Any())
			{
				var tags = await GetOrCreateTagsAsync(dto.Tags);
				foreach (var tag in tags)
				{
					var skillTag = new SkillTag
					{
						SkillId = skill.Id,
						TagId = tag.Id
					};

					_db.SkillTags.Add(skillTag);
				}
				await _db.SaveChangesAsync();
			}
			return skill;
		}

		public async Task<int> GetPendingSkillsCountAsync(string userId)
		{
			return await _db.Skills.CountAsync(s => s.Status == SkillStatus.Pending);
		}
		
		public async Task<Skill?> GetSkillByNameAsync(string name)
		{
			return await _db.Skills
				.Include(s => s.SkillTags)
				.ThenInclude(st => st.Tag)
				.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
		}
		
		public async Task<Skill?> GetSkillByIdAsync(int id)
		{
			return await _db.Skills
				.Include(s => s.SkillTags)
				.ThenInclude(st => st.Tag)
				.FirstOrDefaultAsync(s => s.Id == id);
		}
		
		public async Task<List<Skill>> GetAllSkillsAsync(bool includeApprovedOnly = false)
		{
			var query = _db.Skills
				.Include(s => s.SkillTags)
				.ThenInclude(st => st.Tag)
				.AsQueryable();

			if (includeApprovedOnly)
			{
				query = query.Where(s => s.Status == SkillStatus.Approved);
			}

			return await query.ToListAsync();
		}
		
		public async Task ApproveSkillAsync(int skillId)
		{
			var skill = await _db.Skills.FindAsync(skillId);
			if (skill == null)
			{
				throw new InvalidOperationException("Skill not found.");
			}

			skill.Status = SkillStatus.Approved;

			await _db.SaveChangesAsync();
		}
		
		public async Task ApproveSkillWithEditsAsync(int skillId, SkillModerationDto dto)
		{
			var skill = await _db.Skills
				.Include(s => s.SkillTags)
				.FirstOrDefaultAsync(s => s.Id == skillId);

			if (skill == null)
			{
				throw new InvalidOperationException("Skill not found.");
			}

			// Update skill details
			skill.Name = dto.Name;
			skill.Description = dto.Description;
			skill.Status = SkillStatus.Approved;

			// update tags
			_db.SkillTags.RemoveRange(skill.SkillTags);
			if (dto.Tags.Any())
			{
				var tags = await GetOrCreateTagsAsync(dto.Tags);
				foreach (var tag in tags)
				{
					var skillTag = new SkillTag
					{
						SkillId = skill.Id,
						TagId = tag.Id
					};

					_db.SkillTags.Add(skillTag);
				}
			}

			await _db.SaveChangesAsync();
		}

		public async Task RejectSkillAsync(int skillId)
		{
			var skill = await _db.Skills
				.Include(s => s.SkillTags)
				.FirstOrDefaultAsync(s => s.Id == skillId);

			if (skill == null)
			{
				throw new InvalidOperationException("Skill not found.");
			}

			_db.UserSkills.RemoveRange(skill.UserSkills);

			_db.Skills.Remove(skill); // Remove "Rejected" skill entirely ??

			await _db.SaveChangesAsync();
		}
		
		public async Task MarkSkillAsDuplicateAsync(int skillId, int existingSkillId)
		{
			var duplicateSkill = await _db.Skills
				.Include(s => s.UserSkills)
				.FirstOrDefaultAsync(s => s.Id == skillId);

			var existingSkill = await _db.Skills.FindAsync(existingSkillId);

			if (duplicateSkill == null || existingSkill == null)
			{
				throw new InvalidOperationException("Skill not found.");
			}

			if (existingSkill.Status != SkillStatus.Approved)
			{
				throw new InvalidOperationException("The existing skill must be approved to mark another skill as its duplicate.");
			}

			// Replace all UserSkill references to the duplicate skill with the existing skill
			foreach (var duplicateUserSkill in duplicateSkill.UserSkills)
			{
				// Check if user already has the existing skill to avoid duplicates in UserSkills
				var existingUserSkill = await _db.UserSkills
					.FirstOrDefaultAsync(us => us.UserId == duplicateUserSkill.UserId && us.SkillId == existingSkillId);
				if (existingUserSkill == null)
				{
					duplicateUserSkill.SkillId = existingSkillId;
				}
			}

			// Delete duplicate skill
			_db.Skills.Remove(duplicateSkill);

			await _db.SaveChangesAsync();
		}

		public async Task<List<Tag>> GetOrCreateTagsAsync(List<string> tagNames)
		{
			var tags = new List<Tag>();

			foreach (var tagName in tagNames.Distinct())
			{
				var normalizedName = tagName.Trim().ToLower();
				var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedName);

				if (tag == null)
				{
					tag = new Tag { Name = normalizedName };
					_db.Tags.Add(tag);

					await _db.SaveChangesAsync(); // Save to get the ID for the new tag
				}
				tags.Add(tag);
			}
			return tags;
		}
	}
}
