using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs;
using MentoringApp.Api.DTOs.Auth;
using MentoringApp.Api.DTOs.Mentorship;
using MentoringApp.Api.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // -------------- USERS --------------

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userManager.Users
                .Include(u => u.Profile)
                .AsNoTracking()
                .ToListAsync();

        var result = new List<AdminUserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                Roles = roles,
                LockoutEnd = user.LockoutEnd,
                //CreatedAt = user.CreatedAt,
                //LastLoginAt = user.LastLoginAt
            });
        }

        return Ok(result);
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null) return NotFound();

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded) return BadRequest(result.Errors);

        return NoContent();
    }

    // ------------- MENTORSHIPS ----------------

    [HttpGet("mentorships")]
    public async Task<IActionResult> GetAllmentorships()
    {
        var mentorships = await _context.Mentorships
                .Include(m => m.Mentor)
                .Include(m => m.Mentee)
                .AsNoTracking()
                .ToListAsync();

        return Ok(mentorships.Select(m => new
        {
            m.Id,
            m.Scope,
            m.Status,
            m.StartDate,
            Mentor = m.Mentor.UserName,
            Mentee = m.Mentee.UserName
        }));
    }

    [HttpPatch("mentorships/{id}")]
    public async Task<IActionResult> UpdateMentorshipStatus(int id, [FromBody] UpdateMentorshipDto dto)
    {
        var mentorship = await _context.Mentorships
                .FirstOrDefaultAsync(m => m.Id == id);

        if (mentorship == null)
            return NotFound();

        // Apply updates conditionally
        if (dto.Status != null)
        {
            //if (!AllowStatuses.Contains(dto.Status))
            //    return BadRequest("Invalid status value: " + dto.Status);

            mentorship.Status = dto.Status;
        }

        if (dto.Scope != null) mentorship.Scope = dto.Scope;

        if (dto.EndDate.HasValue)
        {
            if (dto.EndDate < mentorship.StartDate)
                return BadRequest("EndDate cannot be before StartDate");

            mentorship.EndDate = dto.EndDate.Value;
        }
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ------------- SKILLS ----------------

    [HttpGet("skills")]
    public async Task<IActionResult> GetAllSkills()
    {
        var skills = await _context.Skills
                .AsNoTracking()
                .ToListAsync();

        return Ok(skills);
    }

    [HttpPut("skills/{id}")]
    public async Task<IActionResult> UpdateSkill(int id, [FromBody] SkillDto updatedSkill)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null)
            return NotFound();

        skill.Name = updatedSkill.Name;
        await _context.SaveChangesAsync();

        return Ok(skill);
    }

    [HttpDelete("skills/{id}")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null)
            return NotFound();

        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
