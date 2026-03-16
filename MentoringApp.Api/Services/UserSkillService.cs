using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.UserSkills;
using MentoringApp.Api.Enums;
using MentoringApp.Api.Models;
using MentoringApp.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Services
{
	public class UserSkillService : IUserSkillService
	{
		private readonly ApplicationDbContext _db;

		public UserSkillService(ApplicationDbContext db)
		{
			_db = db;
		}

		public async Task AddUserSkillAsync(string userId, AddUserSkillDto dto)
		{
			// Validate that the skill exists and is approved
			var skill = await _db.Skills.FindAsync(dto.SkillId);
			if (skill == null)
			{
				throw new InvalidOperationException("Skill not found.");
			}

			if (skill.Status != SkillStatus.Approved)
			{
				throw new InvalidOperationException("Only approved skills can be added to a user profile.");
			}

			// Prevent duplicate skills for the same user
			bool alreadyExists = await _db.UserSkills
				.AnyAsync(us => us.UserId == userId && us.SkillId == dto.SkillId);

			if (alreadyExists)
			{
				throw new InvalidOperationException("This skill is already assigned to the user.");
			}

			var userSkill = new UserSkill
			{
				UserId = userId,
				SkillId = dto.SkillId,
				Level = dto.Level,
				YearsExperience = dto.YearsExperience
			};

			_db.UserSkills.Add(userSkill);
			await _db.SaveChangesAsync();
		}

		public async Task UpdateUserSkillAsync(string userId, UpdateUserSkillDto dto)
		{
			var userSkill = await _db.UserSkills
				.FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == dto.SkillId);

			if (userSkill == null)
			{
				throw new InvalidOperationException("User skill not found.");
			}

			userSkill.Level = dto.Level;
			userSkill.YearsExperience = dto.YearsExperience;

			await _db.SaveChangesAsync();
		}

		public async Task RemoveUserSkillAsync(string userId, int skillId)
		{
			var userSkill = await _db.UserSkills
				.FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);

			if (userSkill == null)
			{
				throw new InvalidOperationException("User skill not found.");
			}

			_db.UserSkills.Remove(userSkill);
			await _db.SaveChangesAsync();
		}

		public async Task<List<UserSkillDto>> GetUserSkillsAsync(string userId)
		{
			return await _db.UserSkills
				.Where(us => us.UserId == userId)
				.Include(us => us.Skill)
				.Select(us => MapToDto(us))
				.ToListAsync();
		}

		public async Task<UserSkillDto?> GetUserSkillAsync(string userId, int skillId)
		{
			var userSkill = await _db.UserSkills
				.Include(us => us.Skill)
				.FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);

			if (userSkill == null)
			{
				return null;
			}

			return MapToDto(userSkill);
		}

		public async Task<List<UserSkillDto>> GetUsersBySkillAsync(int skillId)
		{
			var skill = await _db.Skills.FindAsync(skillId);
			if (skill == null)
			{
				throw new InvalidOperationException("Skill not found.");
			}

			return await _db.UserSkills
				.Where(us => us.SkillId == skillId)
				.Include(us => us.Skill)
				.Select(us => MapToDto(us))
				.ToListAsync();
		}

		private static UserSkillDto MapToDto(UserSkill userSkill)
		{
			return new UserSkillDto
			{
				Id = userSkill.Id,
				UserId = userSkill.UserId,
				SkillId = userSkill.SkillId,
				SkillName = userSkill.Skill.Name,
				Level = userSkill.Level,
				YearsExperience = userSkill.YearsExperience
			};
		}
	}
}
