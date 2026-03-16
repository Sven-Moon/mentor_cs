using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Skills;
using MentoringApp.Api.Enums;
using MentoringApp.Api.Models;
using MentoringApp.Api.Services;
using MentoringApp.Api.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Tests.Services
{
	public class SkillServiceTests : IDisposable
	{
		private readonly ApplicationDbContext _db;
		private readonly SqliteConnection _connection;
		private readonly SkillService _sut;
		private const string TestUserId = "user-1";

		public SkillServiceTests()
		{
			(_db, _connection) = TestDbContextFactory.Create();
			_sut = new SkillService(_db);
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

		private Skill CreateSkillEntity(
			string name,
			SkillStatus status = SkillStatus.Approved,
			string? description = null)
		{
			return new Skill
			{
				Name = name,
				NormalizedName = name.Trim().ToUpperInvariant(),
				Description = description ?? $"{name} description",
				Status = status
			};
		}

		private async Task<Skill> SeedSkillAsync(
			string name,
			SkillStatus status = SkillStatus.Approved,
			string? description = null)
		{
			var skill = CreateSkillEntity(name, status, description);
			_db.Skills.Add(skill);
			await _db.SaveChangesAsync();
			return skill;
		}

		private async Task<SkillCategory> SeedCategoryAsync(string name)
		{
			var category = new SkillCategory { Name = name };
			_db.SkillCategories.Add(category);
			await _db.SaveChangesAsync();
			return category;
		}

		#endregion

		#region CreateSkillAsync

		[Fact]
		public async Task CreateSkillAsync_ValidDto_CreatesSkillWithPendingStatus()
		{
			var dto = new SkillCreateDto
			{
				Name = "C#",
				Description = "A modern programming language"
			};

			var result = await _sut.CreateSkillAsync(TestUserId, dto);

			Assert.NotNull(result);
			Assert.Equal("C#", result.Name);
			Assert.Equal("C#", result.NormalizedName);
			Assert.Equal(SkillStatus.Pending, result.Status);
			Assert.Equal("A modern programming language", result.Description);

			var persisted = await _db.Skills.FindAsync(result.Id);
			Assert.NotNull(persisted);
		}

		[Fact]
		public async Task CreateSkillAsync_DuplicateName_ThrowsInvalidOperationException()
		{
			await SeedSkillAsync("JavaScript");

			var dto = new SkillCreateDto
			{
				Name = "JavaScript",
				Description = "Duplicate"
			};

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.CreateSkillAsync(TestUserId, dto));

			Assert.Contains("same name already exists", ex.Message);
		}

		[Fact]
		public async Task CreateSkillAsync_DuplicateNameDifferentCase_ThrowsInvalidOperationException()
		{
			await SeedSkillAsync("Python");

			var dto = new SkillCreateDto
			{
				Name = "  python  ",
				Description = "Case-insensitive duplicate"
			};

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.CreateSkillAsync(TestUserId, dto));

			Assert.Contains("same name already exists", ex.Message);
		}

		[Fact]
		public async Task CreateSkillAsync_PendingLimitReached_ThrowsInvalidOperationException()
		{
			// Seed 10 pending skills
			for (int i = 0; i < 10; i++)
			{
				await SeedSkillAsync($"PendingSkill{i}", SkillStatus.Pending);
			}

			var dto = new SkillCreateDto
			{
				Name = "OneMoreSkill",
				Description = "Should be rejected"
			};

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.CreateSkillAsync(TestUserId, dto));

			Assert.Contains("limit of 10 pending skills", ex.Message);
		}

		[Fact]
		public async Task CreateSkillAsync_WithCategories_AssociatesCategoriesCorrectly()
		{
			var cat1 = await SeedCategoryAsync("Backend");
			var cat2 = await SeedCategoryAsync("Languages");

			var dto = new SkillCreateDto
			{
				Name = "Go",
				Description = "Go language",
				CategoryIds = [cat1.Id, cat2.Id]
			};

			var result = await _sut.CreateSkillAsync(TestUserId, dto);

			var skill = await _db.Skills
				.Include(s => s.Categories)
				.FirstAsync(s => s.Id == result.Id);

			Assert.Equal(2, skill.Categories.Count);
		}

		[Fact]
		public async Task CreateSkillAsync_WithTags_CreatesAndAssociatesTagsCorrectly()
		{
			var dto = new SkillCreateDto
			{
				Name = "Docker",
				Description = "Container platform",
				Tags = ["devops", "containers"]
			};

			var result = await _sut.CreateSkillAsync(TestUserId, dto);

			var skill = await _db.Skills
				.Include(s => s.Tags)
				.FirstAsync(s => s.Id == result.Id);

			Assert.Equal(2, skill.Tags.Count);
			Assert.Contains(skill.Tags, t => t.Name == "devops");
			Assert.Contains(skill.Tags, t => t.Name == "containers");
		}

		[Fact]
		public async Task CreateSkillAsync_WithExistingTags_ReusesExistingTags()
		{
			// Pre-create a tag
			_db.Tags.Add(new Tag { Name = "backend" });
			await _db.SaveChangesAsync();

			var dto = new SkillCreateDto
			{
				Name = "Rust",
				Description = "Systems language",
				Tags = ["backend", "systems"]
			};

			await _sut.CreateSkillAsync(TestUserId, dto);

			// Should only have 2 total tags, reusing "backend"
			var allTags = await _db.Tags.ToListAsync();
			Assert.Equal(2, allTags.Count);
		}

		#endregion

		#region GetPendingSkillsCountAsync

		[Fact]
		public async Task GetPendingSkillsCountAsync_ReturnsCorrectCount()
		{
			await SeedSkillAsync("Approved1", SkillStatus.Approved);
			await SeedSkillAsync("Pending1", SkillStatus.Pending);
			await SeedSkillAsync("Pending2", SkillStatus.Pending);

			var count = await _sut.GetPendingSkillsCountAsync(TestUserId);

			Assert.Equal(2, count);
		}

		[Fact]
		public async Task GetPendingSkillsCountAsync_NoPendingSkills_ReturnsZero()
		{
			await SeedSkillAsync("Approved1", SkillStatus.Approved);

			var count = await _sut.GetPendingSkillsCountAsync(TestUserId);

			Assert.Equal(0, count);
		}

		#endregion

		#region GetSkillByNameAsync

		[Fact]
		public async Task GetSkillByNameAsync_ExistingSkill_ReturnsSkillWithRelations()
		{
			var skill = await SeedSkillAsync("TypeScript");

			var result = await _sut.GetSkillByNameAsync("TypeScript");

			Assert.NotNull(result);
			Assert.Equal(skill.Id, result.Id);
		}

		[Fact]
		public async Task GetSkillByNameAsync_NonExistent_ReturnsNull()
		{
			var result = await _sut.GetSkillByNameAsync("NonExistent");

			Assert.Null(result);
		}

		#endregion

		#region GetSkillByIdAsync

		[Fact]
		public async Task GetSkillByIdAsync_ExistingSkill_ReturnsSkillWithRelations()
		{
			var skill = await SeedSkillAsync("Kotlin");

			var result = await _sut.GetSkillByIdAsync(skill.Id);

			Assert.NotNull(result);
			Assert.Equal("Kotlin", result.Name);
		}

		[Fact]
		public async Task GetSkillByIdAsync_NonExistent_ReturnsNull()
		{
			var result = await _sut.GetSkillByIdAsync(9999);

			Assert.Null(result);
		}

		#endregion

		#region GetAllSkillsAsync

		[Fact]
		public async Task GetAllSkillsAsync_ReturnsAllSkills()
		{
			await SeedSkillAsync("Skill1", SkillStatus.Approved);
			await SeedSkillAsync("Skill2", SkillStatus.Pending);

			var result = await _sut.GetAllSkillsAsync();

			Assert.Equal(2, result.Count);
		}

		[Fact]
		public async Task GetAllSkillsAsync_ApprovedOnly_FiltersCorrectly()
		{
			await SeedSkillAsync("ApprovedSkill", SkillStatus.Approved);
			await SeedSkillAsync("PendingSkill", SkillStatus.Pending);
			await SeedSkillAsync("RejectedSkill", SkillStatus.Rejected);

			var result = await _sut.GetAllSkillsAsync(includeApprovedOnly: true);

			Assert.Single(result);
			Assert.Equal("ApprovedSkill", result[0].Name);
		}

		#endregion

		#region ApproveSkillAsync

		[Fact]
		public async Task ApproveSkillAsync_ExistingSkill_SetsStatusToApproved()
		{
			var skill = await SeedSkillAsync("PendingSkill", SkillStatus.Pending);

			await _sut.ApproveSkillAsync(skill.Id);

			var updated = await _db.Skills.FindAsync(skill.Id);
			Assert.Equal(SkillStatus.Approved, updated!.Status);
		}

		[Fact]
		public async Task ApproveSkillAsync_NonExistent_ThrowsInvalidOperationException()
		{
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.ApproveSkillAsync(9999));

			Assert.Contains("Skill not found", ex.Message);
		}

		#endregion

		#region ApproveSkillWithEditsAsync

		[Fact]
		public async Task ApproveSkillWithEditsAsync_UpdatesNameDescriptionAndStatus()
		{
			var skill = await SeedSkillAsync("OldName", SkillStatus.Pending, "Old desc");

			var dto = new SkillModerationDto
			{
				Name = "NewName",
				Description = "New description"
			};

			await _sut.ApproveSkillWithEditsAsync(skill.Id, dto);

			var updated = await _db.Skills.FindAsync(skill.Id);
			Assert.Equal("NewName", updated!.Name);
			Assert.Equal("New description", updated.Description);
			Assert.Equal(SkillStatus.Approved, updated.Status);
		}

		[Fact]
		public async Task ApproveSkillWithEditsAsync_ReplacesTagsEntirely()
		{
			// Create skill with an existing tag
			var skill = CreateSkillEntity("TaggedSkill", SkillStatus.Pending);
			skill.Tags.Add(new Tag { Name = "oldtag" });
			_db.Skills.Add(skill);
			await _db.SaveChangesAsync();

			var dto = new SkillModerationDto
			{
				Name = "TaggedSkill",
				Description = "Updated",
				Tags = ["newtag1", "newtag2"]
			};

			await _sut.ApproveSkillWithEditsAsync(skill.Id, dto);

			var updated = await _db.Skills
				.Include(s => s.Tags)
				.FirstAsync(s => s.Id == skill.Id);

			Assert.Equal(2, updated.Tags.Count);
			Assert.DoesNotContain(updated.Tags, t => t.Name == "oldtag");
			Assert.Contains(updated.Tags, t => t.Name == "newtag1");
		}

		[Fact]
		public async Task ApproveSkillWithEditsAsync_NonExistent_ThrowsInvalidOperationException()
		{
			var dto = new SkillModerationDto
			{
				Name = "Name",
				Description = "Desc"
			};

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.ApproveSkillWithEditsAsync(9999, dto));

			Assert.Contains("Skill not found", ex.Message);
		}

		#endregion

		#region RejectSkillAsync

		[Fact]
		public async Task RejectSkillAsync_RemovesSkillAndAssociatedUserSkills()
		{
			await SeedUserAsync(TestUserId);
			var skill = await SeedSkillAsync("Rejectable", SkillStatus.Pending);
			_db.UserSkills.Add(new UserSkill
			{
				UserId = TestUserId,
				SkillId = skill.Id,
				Level = SkillLevel.Beginner
			});
			await _db.SaveChangesAsync();

			await _sut.RejectSkillAsync(skill.Id);

			Assert.Null(await _db.Skills.FindAsync(skill.Id));
			Assert.Empty(await _db.UserSkills.Where(us => us.SkillId == skill.Id).ToListAsync());
		}

		[Fact]
		public async Task RejectSkillAsync_NonExistent_ThrowsInvalidOperationException()
		{
			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.RejectSkillAsync(9999));

			Assert.Contains("Skill not found", ex.Message);
		}

		#endregion

		#region MarkSkillAsDuplicateAsync

		[Fact]
		public async Task MarkSkillAsDuplicateAsync_MigratesUserSkillsAndRemovesDuplicate()
		{
			await SeedUserAsync(TestUserId);
			var existing = await SeedSkillAsync("ExistingSkill", SkillStatus.Approved);
			var duplicate = await SeedSkillAsync("DuplicateSkill", SkillStatus.Pending);

			_db.UserSkills.Add(new UserSkill
			{
				UserId = TestUserId,
				SkillId = duplicate.Id,
				Level = SkillLevel.Intermediate
			});
			await _db.SaveChangesAsync();

			await _sut.MarkSkillAsDuplicateAsync(duplicate.Id, existing.Id);

			Assert.Null(await _db.Skills.FindAsync(duplicate.Id));

			var userSkill = await _db.UserSkills
				.FirstOrDefaultAsync(us => us.UserId == TestUserId && us.SkillId == existing.Id);
			Assert.NotNull(userSkill);
		}

		[Fact]
		public async Task MarkSkillAsDuplicateAsync_UserAlreadyHasExistingSkill_DoesNotCreateDuplicate()
		{
			await SeedUserAsync(TestUserId);
			var existing = await SeedSkillAsync("ExistingSkill", SkillStatus.Approved);
			var duplicate = await SeedSkillAsync("DuplicateSkill", SkillStatus.Pending);

			// User already has the existing skill
			_db.UserSkills.Add(new UserSkill
			{
				UserId = TestUserId,
				SkillId = existing.Id,
				Level = SkillLevel.Expert
			});
			// User also has the duplicate skill
			_db.UserSkills.Add(new UserSkill
			{
				UserId = TestUserId,
				SkillId = duplicate.Id,
				Level = SkillLevel.Beginner
			});
			await _db.SaveChangesAsync();

			await _sut.MarkSkillAsDuplicateAsync(duplicate.Id, existing.Id);

			// Should still have only one UserSkill for the existing skill
			var userSkills = await _db.UserSkills
				.Where(us => us.UserId == TestUserId && us.SkillId == existing.Id)
				.ToListAsync();
			Assert.Single(userSkills);
		}

		[Fact]
		public async Task MarkSkillAsDuplicateAsync_ExistingSkillNotApproved_ThrowsInvalidOperationException()
		{
			var existing = await SeedSkillAsync("NotApproved", SkillStatus.Pending);
			var duplicate = await SeedSkillAsync("Dup", SkillStatus.Pending);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.MarkSkillAsDuplicateAsync(duplicate.Id, existing.Id));

			Assert.Contains("must be approved", ex.Message);
		}

		[Fact]
		public async Task MarkSkillAsDuplicateAsync_SkillNotFound_ThrowsInvalidOperationException()
		{
			var existing = await SeedSkillAsync("Exists", SkillStatus.Approved);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _sut.MarkSkillAsDuplicateAsync(9999, existing.Id));

			Assert.Contains("Skill not found", ex.Message);
		}

		#endregion

		#region GetOrCreateTagsAsync

		[Fact]
		public async Task GetOrCreateTagsAsync_AllNew_CreatesAllTags()
		{
			var result = await _sut.GetOrCreateTagsAsync(["frontend", "react"]);

			Assert.Equal(2, result.Count);
			Assert.Equal(2, await _db.Tags.CountAsync());
		}

		[Fact]
		public async Task GetOrCreateTagsAsync_MixedExistingAndNew_ReusesExistingCreatesNew()
		{
			_db.Tags.Add(new Tag { Name = "backend" });
			await _db.SaveChangesAsync();

			var result = await _sut.GetOrCreateTagsAsync(["backend", "api"]);

			Assert.Equal(2, result.Count);
			Assert.Equal(2, await _db.Tags.CountAsync());
		}

		[Fact]
		public async Task GetOrCreateTagsAsync_DuplicateInputs_DeduplicatesCorrectly()
		{
			var result = await _sut.GetOrCreateTagsAsync(["cloud", "CLOUD", " Cloud "]);

			Assert.Single(result);
			Assert.Single(await _db.Tags.ToListAsync());
		}

		#endregion
	}
}
