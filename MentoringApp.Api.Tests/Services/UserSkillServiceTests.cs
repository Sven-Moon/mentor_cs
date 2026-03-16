using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.UserSkills;
using MentoringApp.Api.Enums;
using MentoringApp.Api.Models;
using MentoringApp.Api.Services;
using MentoringApp.Api.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Tests.Services
{
	public class UserSkillServiceTests : IDisposable
	{
		private readonly ApplicationDbContext _db;
		private readonly SqliteConnection _connection;
		private readonly UserSkillService _sut;
		private const string UserId = "user-1";
		private const string OtherUserId = "user-2";

		public UserSkillServiceTests()
		{
			(_db, _connection) = TestDbContextFactory.Create();
			_sut = new UserSkillService(_db);
		}

		public void Dispose()
		{
			_db.Dispose();
			_connection.Dispose();
		}

		#region Helpers

		/// <summary>
		/// Seeds a minimal IdentityUser row so that UserSkill foreign keys resolve.
		/// </summary>
		private async Task SeedUserAsync(string userId)
		{
			var user = new IdentityUser
			{
				Id = userId,
				UserName = userId,
				NormalizedUserName = userId.ToUpperInvariant(),
				Email = $"{userId}@test.com",
				NormalizedEmail = $"{userId}@test.com".ToUpperInvariant(),
				SecurityStamp = Guid.NewGuid().ToString()
			};
			_db.Users.Add(user);
			await _db.SaveChangesAsync();
		}

		private async Task<Skill> SeedApprovedSkillAsync(string name)
		{
			var skill = new Skill
			{
				Name = name,
				NormalizedName = name.Trim().ToUpperInvariant(),
				Description = $"{name} description",
				Status = SkillStatus.Approved
			};
			_db.Skills.Add(skill);
			await _db.SaveChangesAsync();
			return skill;
		}

		private async Task<Skill> SeedPendingSkillAsync(string name)
		{
			var skill = new Skill
			{
				Name = name,
				NormalizedName = name.Trim().ToUpperInvariant(),
				Description = $"{name} description",
				Status = SkillStatus.Pending
			};
			_db.Skills.Add(skill);
			await _db.SaveChangesAsync();
			return skill;
		}

		private async Task<UserSkill> SeedUserSkillAsync(
			string userId,
			int skillId,
			SkillLevel level = SkillLevel.Intermediate,
			int? yearsExperience = null)
		{
			var userSkill = new UserSkill
			{
				UserId = userId,
				SkillId = skillId,
				Level = level,
				YearsExperience = yearsExperience
			};
			_db.UserSkills.Add(userSkill);
			await _db.SaveChangesAsync();
			return userSkill;
		}

		#endregion

		#region AddUserSkillAsync

		[Fact]
		public async Task AddUserSkillAsync_ValidDto_CreatesUserSkill()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("C#");

			var dto = new AddUserSkillDto
			{
				SkillId = skill.Id,
				Level = SkillLevel.Advanced,
				YearsExperience = 5
			};

			await _sut.AddUserSkillAsync(UserId, dto);

			var userSkill = await _db.UserSkills
				.FirstOrDefaultAsync(us => us.UserId == UserId && us.SkillId == skill.Id);

			Assert.NotNull(userSkill);
			Assert.Equal(SkillLevel.Advanced, userSkill.Level);
			Assert.Equal(5, userSkill.YearsExperience);
		}

		[Fact]
		public async Task AddUserSkillAsync_NullYearsExperience_CreatesUserSkillWithNullYears()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("Go");

			var dto = new AddUserSkillDto
			{
				SkillId = skill.Id,
				Level = SkillLevel.Beginner,
				YearsExperience = null
			};

			await _sut.AddUserSkillAsync(UserId, dto);

			var userSkill = await _db.UserSkills
				.FirstOrDefaultAsync(us => us.UserId == UserId && us.SkillId == skill.Id);

			Assert.NotNull(userSkill);
			Assert.Null(userSkill.YearsExperience);
		}

		[Fact]
		public async Task AddUserSkillAsync_SkillNotFound_ThrowsInvalidOperationException()
		{
			var dto = new AddUserSkillDto
			{
				SkillId = 9999,
				Level = SkillLevel.Beginner
			};

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.AddUserSkillAsync(UserId, dto));

			Assert.Contains("Skill not found", ex.Message);
		}

		[Fact]
		public async Task AddUserSkillAsync_SkillNotApproved_ThrowsInvalidOperationException()
		{
			var skill = await SeedPendingSkillAsync("PendingSkill");

			var dto = new AddUserSkillDto
			{
				SkillId = skill.Id,
				Level = SkillLevel.Beginner
			};

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.AddUserSkillAsync(UserId, dto));

			Assert.Contains("Only approved skills", ex.Message);
		}

		[Fact]
		public async Task AddUserSkillAsync_DuplicateSkillForUser_ThrowsInvalidOperationException()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("JavaScript");
			await SeedUserSkillAsync(UserId, skill.Id);

			var dto = new AddUserSkillDto
			{
				SkillId = skill.Id,
				Level = SkillLevel.Expert
			};

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.AddUserSkillAsync(UserId, dto));

			Assert.Contains("already assigned", ex.Message);
		}

		[Fact]
		public async Task AddUserSkillAsync_SameSkillDifferentUsers_Succeeds()
		{
			await SeedUserAsync(UserId);
			await SeedUserAsync(OtherUserId);
			var skill = await SeedApprovedSkillAsync("Python");
			await SeedUserSkillAsync(UserId, skill.Id);

			var dto = new AddUserSkillDto
			{
				SkillId = skill.Id,
				Level = SkillLevel.Intermediate
			};

			// A different user adding the same skill should succeed
			await _sut.AddUserSkillAsync(OtherUserId, dto);

			var count = await _db.UserSkills
				.CountAsync(us => us.SkillId == skill.Id);
			Assert.Equal(2, count);
		}

		#endregion

		#region UpdateUserSkillAsync

		[Fact]
		public async Task UpdateUserSkillAsync_ExistingUserSkill_UpdatesLevelAndYears()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("TypeScript");
			await SeedUserSkillAsync(UserId, skill.Id, SkillLevel.Beginner, 1);

			var dto = new UpdateUserSkillDto
			{
				SkillId = skill.Id,
				Level = SkillLevel.Expert,
				YearsExperience = 10
			};

			await _sut.UpdateUserSkillAsync(UserId, dto);

			var updated = await _db.UserSkills
				.FirstAsync(us => us.UserId == UserId && us.SkillId == skill.Id);

			Assert.Equal(SkillLevel.Expert, updated.Level);
			Assert.Equal(10, updated.YearsExperience);
		}

		[Fact]
		public async Task UpdateUserSkillAsync_ClearYearsExperience_SetsToNull()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("Rust");
			await SeedUserSkillAsync(UserId, skill.Id, SkillLevel.Intermediate, 3);

			var dto = new UpdateUserSkillDto
			{
				SkillId = skill.Id,
				Level = SkillLevel.Advanced,
				YearsExperience = null
			};

			await _sut.UpdateUserSkillAsync(UserId, dto);

			var updated = await _db.UserSkills
				.FirstAsync(us => us.UserId == UserId && us.SkillId == skill.Id);

			Assert.Null(updated.YearsExperience);
		}

		[Fact]
		public async Task UpdateUserSkillAsync_NonExistent_ThrowsInvalidOperationException()
		{
			var dto = new UpdateUserSkillDto
			{
				SkillId = 9999,
				Level = SkillLevel.Beginner
			};

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.UpdateUserSkillAsync(UserId, dto));

			Assert.Contains("User skill not found", ex.Message);
		}

		[Fact]
		public async Task UpdateUserSkillAsync_WrongUser_ThrowsInvalidOperationException()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("Kotlin");
			await SeedUserSkillAsync(UserId, skill.Id);

			var dto = new UpdateUserSkillDto
			{
				SkillId = skill.Id,
				Level = SkillLevel.Expert
			};

			// OtherUserId does not have this skill
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.UpdateUserSkillAsync(OtherUserId, dto));

			Assert.Contains("User skill not found", ex.Message);
		}

		#endregion

		#region RemoveUserSkillAsync

		[Fact]
		public async Task RemoveUserSkillAsync_ExistingUserSkill_RemovesSuccessfully()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("Docker");
			await SeedUserSkillAsync(UserId, skill.Id);

			await _sut.RemoveUserSkillAsync(UserId, skill.Id);

			var removed = await _db.UserSkills
				.FirstOrDefaultAsync(us => us.UserId == UserId && us.SkillId == skill.Id);

			Assert.Null(removed);
		}

		[Fact]
		public async Task RemoveUserSkillAsync_NonExistent_ThrowsInvalidOperationException()
		{
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.RemoveUserSkillAsync(UserId, 9999));

			Assert.Contains("User skill not found", ex.Message);
		}

		[Fact]
		public async Task RemoveUserSkillAsync_DoesNotAffectOtherUsersSkills()
		{
			await SeedUserAsync(UserId);
			await SeedUserAsync(OtherUserId);
			var skill = await SeedApprovedSkillAsync("Kubernetes");
			await SeedUserSkillAsync(UserId, skill.Id);
			await SeedUserSkillAsync(OtherUserId, skill.Id);

			await _sut.RemoveUserSkillAsync(UserId, skill.Id);

			// OtherUser's skill should remain
			var otherUserSkill = await _db.UserSkills
				.FirstOrDefaultAsync(us => us.UserId == OtherUserId && us.SkillId == skill.Id);
			Assert.NotNull(otherUserSkill);
		}

		#endregion

		#region GetUserSkillsAsync

		[Fact]
		public async Task GetUserSkillsAsync_ReturnsAllSkillsForUser()
		{
			await SeedUserAsync(UserId);
			var skill1 = await SeedApprovedSkillAsync("React");
			var skill2 = await SeedApprovedSkillAsync("Angular");
			await SeedUserSkillAsync(UserId, skill1.Id, SkillLevel.Advanced, 4);
			await SeedUserSkillAsync(UserId, skill2.Id, SkillLevel.Beginner, 1);

			var result = await _sut.GetUserSkillsAsync(UserId);

			Assert.Equal(2, result.Count);
			Assert.All(result, dto => Assert.Equal(UserId, dto.UserId));
		}

		[Fact]
		public async Task GetUserSkillsAsync_IncludesSkillName()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("Vue.js");
			await SeedUserSkillAsync(UserId, skill.Id, SkillLevel.Intermediate);

			var result = await _sut.GetUserSkillsAsync(UserId);

			Assert.Single(result);
			Assert.Equal("Vue.js", result[0].SkillName);
		}

		[Fact]
		public async Task GetUserSkillsAsync_UserHasNoSkills_ReturnsEmptyList()
		{
			var result = await _sut.GetUserSkillsAsync(UserId);

			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public async Task GetUserSkillsAsync_DoesNotReturnOtherUsersSkills()
		{
			await SeedUserAsync(OtherUserId);
			var skill = await SeedApprovedSkillAsync("SQL");
			await SeedUserSkillAsync(OtherUserId, skill.Id);

			var result = await _sut.GetUserSkillsAsync(UserId);

			Assert.Empty(result);
		}

		[Fact]
		public async Task GetUserSkillsAsync_MapsAllDtoFieldsCorrectly()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("GraphQL");
			var userSkill = await SeedUserSkillAsync(UserId, skill.Id, SkillLevel.Expert, 7);

			var result = await _sut.GetUserSkillsAsync(UserId);

			Assert.Single(result);
			var dto = result[0];
			Assert.Equal(userSkill.Id, dto.Id);
			Assert.Equal(UserId, dto.UserId);
			Assert.Equal(skill.Id, dto.SkillId);
			Assert.Equal("GraphQL", dto.SkillName);
			Assert.Equal(SkillLevel.Expert, dto.Level);
			Assert.Equal(7, dto.YearsExperience);
		}

		#endregion

		#region GetUserSkillAsync

		[Fact]
		public async Task GetUserSkillAsync_ExistingRecord_ReturnsDtoWithSkillName()
		{
			await SeedUserAsync(UserId);
			var skill = await SeedApprovedSkillAsync("Redis");
			await SeedUserSkillAsync(UserId, skill.Id, SkillLevel.Advanced, 3);

			var result = await _sut.GetUserSkillAsync(UserId, skill.Id);

			Assert.NotNull(result);
			Assert.Equal("Redis", result.SkillName);
			Assert.Equal(SkillLevel.Advanced, result.Level);
			Assert.Equal(3, result.YearsExperience);
		}

		[Fact]
		public async Task GetUserSkillAsync_NonExistentSkill_ReturnsNull()
		{
			var result = await _sut.GetUserSkillAsync(UserId, 9999);

			Assert.Null(result);
		}

		[Fact]
		public async Task GetUserSkillAsync_WrongUser_ReturnsNull()
		{
			await SeedUserAsync(OtherUserId);
			var skill = await SeedApprovedSkillAsync("MongoDB");
			await SeedUserSkillAsync(OtherUserId, skill.Id);

			var result = await _sut.GetUserSkillAsync(UserId, skill.Id);

			Assert.Null(result);
		}

		#endregion

		#region GetUsersBySkillAsync

		[Fact]
		public async Task GetUsersBySkillAsync_ReturnsAllUsersWithSkill()
		{
			await SeedUserAsync(UserId);
			await SeedUserAsync(OtherUserId);
			var skill = await SeedApprovedSkillAsync("Terraform");
			await SeedUserSkillAsync(UserId, skill.Id, SkillLevel.Beginner);
			await SeedUserSkillAsync(OtherUserId, skill.Id, SkillLevel.Expert);

			var result = await _sut.GetUsersBySkillAsync(skill.Id);

			Assert.Equal(2, result.Count);
			Assert.Contains(result, dto => dto.UserId == UserId);
			Assert.Contains(result, dto => dto.UserId == OtherUserId);
		}

		[Fact]
		public async Task GetUsersBySkillAsync_SkillNotFound_ThrowsInvalidOperationException()
		{
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.GetUsersBySkillAsync(9999));

			Assert.Contains("Skill not found", ex.Message);
		}

		[Fact]
		public async Task GetUsersBySkillAsync_NoUsersHaveSkill_ReturnsEmptyList()
		{
			var skill = await SeedApprovedSkillAsync("Haskell");

			var result = await _sut.GetUsersBySkillAsync(skill.Id);

			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public async Task GetUsersBySkillAsync_DoesNotReturnUsersWithDifferentSkills()
		{
			await SeedUserAsync(UserId);
			await SeedUserAsync(OtherUserId);
			var skill1 = await SeedApprovedSkillAsync("Elixir");
			var skill2 = await SeedApprovedSkillAsync("Erlang");
			await SeedUserSkillAsync(UserId, skill1.Id);
			await SeedUserSkillAsync(OtherUserId, skill2.Id);

			var result = await _sut.GetUsersBySkillAsync(skill1.Id);

			Assert.Single(result);
			Assert.Equal(UserId, result[0].UserId);
		}

		#endregion
	}
}
