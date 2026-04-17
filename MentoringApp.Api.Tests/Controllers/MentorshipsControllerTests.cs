using MentoringApp.Api.DTOs.Mentorship;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using MentoringApp.Api.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace MentoringApp.Api.Tests.Controllers;

public class MentorshipsControllerTests
{
	// ----------------------------------------------------------------
	// Shared seed helpers
	// ----------------------------------------------------------------

	private static (ApplicationUser mentor, ApplicationUser mentee) SeedUsers(
			MentoringApp.Api.Data.ApplicationDbContext context,
			string mentorId = "mentor-1",
			string menteeId = "mentee-1")
	{
		var mentor = new ApplicationUser { Id = mentorId, UserName = mentorId };
		var mentee = new ApplicationUser { Id = menteeId, UserName = menteeId };
		context.Users.AddRange(mentor, mentee);
		return (mentor, mentee);
	}

	private static Mentorship SeedMentorship(
			MentoringApp.Api.Data.ApplicationDbContext context,
			string mentorId,
			string menteeId,
			string scope = "Test Scope",
			string status = "Active")
	{
		var m = new Mentorship { MentorId = mentorId, MenteeId = menteeId, Scope = scope, Status = status };
		context.Mentorships.Add(m);
		return m;
	}

	// ----------------------------------------------------------------
	// GET /api/mentorships
	// ----------------------------------------------------------------

	[Fact]
	public async Task GetMentorships_Admin_ReturnsAll()
	{
		using var context = DbContextFactory.CreateContext();

		var (m1, e1) = SeedUsers(context, "a", "b");
		var (m2, e2) = SeedUsers(context, "c", "d");
		SeedMentorship(context, m1.Id, e1.Id, "Scope1");
		SeedMentorship(context, m2.Id, e2.Id, "Scope2");
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "admin", isAdmin: true);

		var result = await controller.GetMentorships();

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var data = Assert.IsAssignableFrom<IEnumerable<Mentorship>>(ok.Value);
		Assert.Equal(2, data.Count());
	}

	[Fact]
	public async Task GetMentorships_NonAdmin_ReturnsOnlyOwn()
	{
		using var context = DbContextFactory.CreateContext();

		var (mentor, mentee) = SeedUsers(context, "me", "other");
		var (m2, e2) = SeedUsers(context, "stranger1", "stranger2");
		SeedMentorship(context, mentor.Id, mentee.Id, "Mine");
		SeedMentorship(context, m2.Id, e2.Id, "NotMine");
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "me");

		var result = await controller.GetMentorships();

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var data = Assert.IsAssignableFrom<IEnumerable<Mentorship>>(ok.Value);
		Assert.Single(data);
		Assert.All(data, m => Assert.True(m.MentorId == "me" || m.MenteeId == "me"));
	}

	[Fact]
	public async Task GetMentorships_AsMentee_ReturnsOwnMentorship()
	{
		using var context = DbContextFactory.CreateContext();

		var (mentor, mentee) = SeedUsers(context, "mentor-x", "mentee-x");
		SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "mentee-x");

		var result = await controller.GetMentorships();

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var data = Assert.IsAssignableFrom<IEnumerable<Mentorship>>(ok.Value);
		Assert.Single(data);
	}

	// ----------------------------------------------------------------
	// GET /api/mentorships/{id}
	// ----------------------------------------------------------------

	[Fact]
	public async Task GetMentorship_MentorAccess_ReturnsOk()
	{
		using var context = DbContextFactory.CreateContext();

		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.GetMentorship(ms.Id);

		Assert.IsType<OkObjectResult>(result.Result);
	}

	[Fact]
	public async Task GetMentorship_NotFound_Returns404()
	{
		using var context = DbContextFactory.CreateContext();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "anyone", isAdmin: true);

		var result = await controller.GetMentorship(999);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task GetMentorship_NonParticipant_ReturnsForbid()
	{
		using var context = DbContextFactory.CreateContext();

		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "outsider");

		var result = await controller.GetMentorship(ms.Id);

		Assert.IsType<ForbidResult>(result.Result);
	}

	// ----------------------------------------------------------------
	// POST /api/mentorships (admin only)
	// ----------------------------------------------------------------

	[Fact]
	public async Task CreateMentorship_Admin_ReturnsCreated()
	{
		using var context = DbContextFactory.CreateContext();
		SeedUsers(context);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "admin", isAdmin: true);

		var dto = new MentorshipDto
		{
			MentorId = "mentor-1",
			MenteeId = "mentee-1",
			Scope = "C# fundamentals",
			Status = "Active",
			StartDate = DateTime.UtcNow,
			EndDate = DateTime.UtcNow.AddMonths(6),
			Version = new byte[4]
		};

		var result = await controller.CreateMentorship(dto);

		var created = Assert.IsType<CreatedAtActionResult>(result.Result);
		var returned = Assert.IsType<MentorshipDto>(created.Value);
		Assert.True(returned.Id > 0);
		Assert.Equal("mentor-1", returned.MentorId);
	}

	// ----------------------------------------------------------------
	// PUT /api/mentorships/{id}
	// ----------------------------------------------------------------

	[Fact]
	public async Task UpdateMentorship_Participant_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var dto = new MentorshipDto
		{
			Id = ms.Id,
			MentorId = mentor.Id,
			MenteeId = mentee.Id,
			Scope = "Updated scope",
			Status = "Completed",
			StartDate = DateTime.UtcNow,
			EndDate = DateTime.UtcNow.AddMonths(1),
			Version = new byte[4]
		};

		var result = await controller.UpdateMentorship(ms.Id, dto);

		Assert.IsType<NoContentResult>(result);
	}

	[Fact]
	public async Task UpdateMentorship_IdMismatch_ReturnsBadRequest()
	{
		using var context = DbContextFactory.CreateContext();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "anyone");

		var dto = new MentorshipDto
		{
			Id = 999,
			MentorId = "x",
			MenteeId = "y",
			Scope = "s",
			Status = "Active",
			Version = new byte[4]
		};

		var result = await controller.UpdateMentorship(1, dto);

		Assert.IsType<BadRequestResult>(result);
	}

	[Fact]
	public async Task UpdateMentorship_NotFound_Returns404()
	{
		using var context = DbContextFactory.CreateContext();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "anyone", isAdmin: true);

		var dto = new MentorshipDto
		{
			Id = 999,
			MentorId = "x",
			MenteeId = "y",
			Scope = "s",
			Status = "Active",
			Version = new byte[4]
		};

		var result = await controller.UpdateMentorship(999, dto);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task UpdateMentorship_NonParticipant_ReturnsForbid()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "outsider");

		var dto = new MentorshipDto
		{
			Id = ms.Id,
			MentorId = mentor.Id,
			MenteeId = mentee.Id,
			Scope = "Changed",
			Status = "Active",
			Version = new byte[4]
		};

		var result = await controller.UpdateMentorship(ms.Id, dto);

		Assert.IsType<ForbidResult>(result);
	}

	// ----------------------------------------------------------------
	// DELETE /api/mentorships/{id} (admin only)
	// ----------------------------------------------------------------

	[Fact]
	public async Task DeleteMentorship_Admin_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "admin", isAdmin: true);

		var result = await controller.DeleteMentorship(ms.Id);

		Assert.IsType<NoContentResult>(result);
		Assert.Null(await context.Mentorships.FindAsync(ms.Id));
	}

	[Fact]
	public async Task DeleteMentorship_NotFound_Returns404()
	{
		using var context = DbContextFactory.CreateContext();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "admin", isAdmin: true);

		var result = await controller.DeleteMentorship(999);

		Assert.IsType<NotFoundResult>(result);
	}

	// ----------------------------------------------------------------
	// GOALS
	// ----------------------------------------------------------------

	[Fact]
	public async Task GetGoals_Participant_ReturnsGoals()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		context.Goals.Add(new Goal { MentorshipId = ms.Id, Title = "Goal A", Status = GoalStatus.Pending, CreatedAt = DateTime.UtcNow });
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.GetGoals(ms.Id);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var data = Assert.IsAssignableFrom<IEnumerable<GoalDto>>(ok.Value);
		Assert.Single(data);
	}

	[Fact]
	public async Task GetGoals_NonParticipant_ReturnsForbid()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "outsider");

		var result = await controller.GetGoals(ms.Id);

		Assert.IsType<ForbidResult>(result.Result);
	}

	[Fact]
	public async Task CreateGoal_Participant_ReturnsCreated()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var dto = new CreateGoalDto { Title = "Learn EF Core", Description = "Study basics" };

		var result = await controller.CreateGoal(ms.Id, dto);

		var created = Assert.IsType<CreatedAtActionResult>(result.Result);
		var returned = Assert.IsType<GoalDto>(created.Value);
		Assert.Equal("Learn EF Core", returned.Title);
		Assert.Equal("Pending", returned.Status);
	}

	[Fact]
	public async Task UpdateGoal_NotFound_Returns404()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.UpdateGoal(ms.Id, 999, new UpdateGoalDto { Title = "X" });

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task UpdateGoal_Participant_UpdatesAndReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var goal = new Goal { MentorshipId = ms.Id, Title = "Old", Status = GoalStatus.Pending, CreatedAt = DateTime.UtcNow };
		context.Goals.Add(goal);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.UpdateGoal(ms.Id, goal.Id, new UpdateGoalDto
		{
			Title = "New",
			Status = GoalStatus.Complete,
			CompletedAt = DateTime.UtcNow
		});

		Assert.IsType<NoContentResult>(result);
		Assert.Equal("New", context.Goals.Find(goal.Id)!.Title);
	}

	[Fact]
	public async Task DeleteGoal_Participant_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var goal = new Goal { MentorshipId = ms.Id, Title = "To delete", Status = GoalStatus.Pending, CreatedAt = DateTime.UtcNow };
		context.Goals.Add(goal);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.DeleteGoal(ms.Id, goal.Id);

		Assert.IsType<NoContentResult>(result);
		Assert.Null(context.Goals.Find(goal.Id));
	}

	// ----------------------------------------------------------------
	// MILESTONES
	// ----------------------------------------------------------------

	[Fact]
	public async Task GetMilestones_Participant_ReturnsMilestones()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		context.Milestones.Add(new Milestone { MentorshipId = ms.Id, Title = "MS1", CreatedAt = DateTime.UtcNow });
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentee.Id);

		var result = await controller.GetMilestones(ms.Id);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var data = Assert.IsAssignableFrom<IEnumerable<MilestoneDto>>(ok.Value);
		Assert.Single(data);
	}

	[Fact]
	public async Task CreateMilestone_Participant_ReturnsCreated()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.CreateMilestone(ms.Id, new CreateMilestoneDto { Title = "Phase 1" });

		var created = Assert.IsType<CreatedAtActionResult>(result.Result);
		var returned = Assert.IsType<MilestoneDto>(created.Value);
		Assert.Equal("Phase 1", returned.Title);
	}

	[Fact]
	public async Task UpdateMilestone_NotFound_Returns404()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.UpdateMilestone(ms.Id, 999, new UpdateMilestoneDto { Title = "X" });

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task DeleteMilestone_Participant_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var milestone = new Milestone { MentorshipId = ms.Id, Title = "Remove me", CreatedAt = DateTime.UtcNow };
		context.Milestones.Add(milestone);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.DeleteMilestone(ms.Id, milestone.Id);

		Assert.IsType<NoContentResult>(result);
		Assert.Null(context.Milestones.Find(milestone.Id));
	}

	// ----------------------------------------------------------------
	// SESSIONS
	// ----------------------------------------------------------------

	[Fact]
	public async Task GetSessions_Participant_ReturnsSessions()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		context.Sessions.Add(new Session
		{
			MentorshipId = ms.Id,
			ScheduledAt = DateTime.UtcNow,
			CreatedAt = DateTime.UtcNow
		});
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.GetSessions(ms.Id);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var data = Assert.IsAssignableFrom<IEnumerable<SessionDto>>(ok.Value);
		Assert.Single(data);
	}

	[Fact]
	public async Task CreateSession_Participant_ReturnsCreated()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var dto = new CreateSessionDto
		{
			Title = "Intro session",
			ScheduledAt = DateTime.UtcNow.AddDays(1),
			Duration = 60,
			Notes = "First meeting"
		};

		var result = await controller.CreateSession(ms.Id, dto);

		var created = Assert.IsType<CreatedAtActionResult>(result.Result);
		var returned = Assert.IsType<SessionDto>(created.Value);
		Assert.Equal("Intro session", returned.Title);
		Assert.Equal(60, returned.Duration);
	}

	[Fact]
	public async Task UpdateSession_NotFound_Returns404()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.UpdateSession(ms.Id, 999, new UpdateSessionDto { ScheduledAt = DateTime.UtcNow });

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task DeleteSession_Participant_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var session = new Session { MentorshipId = ms.Id, ScheduledAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
		context.Sessions.Add(session);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.DeleteSession(ms.Id, session.Id);

		Assert.IsType<NoContentResult>(result);
		Assert.Null(context.Sessions.Find(session.Id));
	}

	// ----------------------------------------------------------------
	// NOTES
	// ----------------------------------------------------------------

	[Fact]
	public async Task GetNotes_Participant_ReturnsNotes()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		context.Notes.Add(new Note
		{
			MentorshipId = ms.Id,
			AuthorId = mentor.Id,
			Content = "Great progress",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		});
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.GetNotes(ms.Id);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var data = Assert.IsAssignableFrom<IEnumerable<NoteDto>>(ok.Value);
		Assert.Single(data);
	}

	[Fact]
	public async Task CreateNote_Participant_SetsAuthorIdFromClaims()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.CreateNote(ms.Id, new CreateNoteDto { Content = "Hello" });

		var created = Assert.IsType<CreatedAtActionResult>(result.Result);
		var returned = Assert.IsType<NoteDto>(created.Value);
		Assert.Equal(mentor.Id, returned.AuthorId);
		Assert.Equal("Hello", returned.Content);
	}

	[Fact]
	public async Task UpdateNote_NonAuthor_ReturnsForbid()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var note = new Note
		{
			MentorshipId = ms.Id,
			AuthorId = mentor.Id,
			Content = "Original",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context.Notes.Add(note);
		await context.SaveChangesAsync();

		// mentee is a participant but not the author
		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentee.Id);

		var result = await controller.UpdateNote(ms.Id, note.Id, new UpdateNoteDto { Content = "Tampered" });

		Assert.IsType<ForbidResult>(result);
	}

	[Fact]
	public async Task UpdateNote_Author_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var note = new Note
		{
			MentorshipId = ms.Id,
			AuthorId = mentor.Id,
			Content = "Original",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context.Notes.Add(note);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.UpdateNote(ms.Id, note.Id, new UpdateNoteDto { Content = "Updated" });

		Assert.IsType<NoContentResult>(result);
		Assert.Equal("Updated", context.Notes.Find(note.Id)!.Content);
	}

	[Fact]
	public async Task UpdateNote_Admin_CanEditAnyNote()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var note = new Note
		{
			MentorshipId = ms.Id,
			AuthorId = mentor.Id,
			Content = "Original",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context.Notes.Add(note);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: "admin", isAdmin: true);

		var result = await controller.UpdateNote(ms.Id, note.Id, new UpdateNoteDto { Content = "Admin edit" });

		Assert.IsType<NoContentResult>(result);
	}

	[Fact]
	public async Task DeleteNote_NonAuthor_ReturnsForbid()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var note = new Note
		{
			MentorshipId = ms.Id,
			AuthorId = mentor.Id,
			Content = "Private",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context.Notes.Add(note);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentee.Id);

		var result = await controller.DeleteNote(ms.Id, note.Id);

		Assert.IsType<ForbidResult>(result);
	}

	[Fact]
	public async Task DeleteNote_Author_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var note = new Note
		{
			MentorshipId = ms.Id,
			AuthorId = mentor.Id,
			Content = "Delete me",
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context.Notes.Add(note);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.DeleteNote(ms.Id, note.Id);

		Assert.IsType<NoContentResult>(result);
		Assert.Null(context.Notes.Find(note.Id));
	}

	// ----------------------------------------------------------------
	// SKILLS
	// ----------------------------------------------------------------

	[Fact]
	public async Task GetSkills_Participant_ReturnsSkills()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		var skill = new Skill { Name = "C#", NormalizedName = "c#", Description = "C# programming" };
		context.Skills.Add(skill);
		await context.SaveChangesAsync();

		ms.Skills.Add(skill);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.GetSkills(ms.Id);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var data = Assert.IsAssignableFrom<IEnumerable<MentoringApp.Api.DTOs.Skills.SkillDto>>(ok.Value);
		Assert.Single(data);
		Assert.Equal("C#", data.First().Name);
	}

	[Fact]
	public async Task AddSkill_NewSkill_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		var skill = new Skill { Name = "SQL", NormalizedName = "sql", Description = "Database" };
		context.Skills.Add(skill);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.AddSkill(ms.Id, skill.Id);

		Assert.IsType<NoContentResult>(result);
		var updated = await context.Mentorships
				.Include(m => m.Skills)
				.FirstAsync(m => m.Id == ms.Id);
		Assert.Contains(updated.Skills, s => s.Id == skill.Id);
	}

	[Fact]
	public async Task AddSkill_AlreadyLinked_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		var skill = new Skill { Name = "Git", NormalizedName = "git", Description = "Version control" };
		context.Skills.Add(skill);
		await context.SaveChangesAsync();

		ms.Skills.Add(skill);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.AddSkill(ms.Id, skill.Id);

		// Idempotent — should still succeed without duplicating
		Assert.IsType<NoContentResult>(result);
	}

	[Fact]
	public async Task AddSkill_SkillNotFound_Returns404()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.AddSkill(ms.Id, 999);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task RemoveSkill_LinkedSkill_ReturnsNoContent()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		var skill = new Skill { Name = "Docker", NormalizedName = "docker", Description = "Containers" };
		context.Skills.Add(skill);
		await context.SaveChangesAsync();

		ms.Skills.Add(skill);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.RemoveSkill(ms.Id, skill.Id);

		Assert.IsType<NoContentResult>(result);
		var updated = await context.Mentorships
				.Include(m => m.Skills)
				.FirstAsync(m => m.Id == ms.Id);
		Assert.DoesNotContain(updated.Skills, s => s.Id == skill.Id);
	}

	[Fact]
	public async Task RemoveSkill_NotLinked_Returns404()
	{
		using var context = DbContextFactory.CreateContext();
		var (mentor, mentee) = SeedUsers(context);
		var ms = SeedMentorship(context, mentor.Id, mentee.Id);
		var skill = new Skill { Name = "Kubernetes", NormalizedName = "kubernetes", Description = "Orchestration" };
		context.Skills.Add(skill);
		await context.SaveChangesAsync();

		var controller = ControllerTestHelper.Create(
				context, MockUserManager<ApplicationUser>.Create().Object,
				userId: mentor.Id);

		var result = await controller.RemoveSkill(ms.Id, skill.Id);

		Assert.IsType<NotFoundResult>(result);
	}
}
