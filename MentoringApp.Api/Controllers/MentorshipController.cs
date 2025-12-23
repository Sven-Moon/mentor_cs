using MentoringApp.Api.Data;
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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MentorshipsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private bool IsAdmin =>
            User.IsInRole("Admin");

        /// <summary>
        /// Get mentorships for the current user (mentor or mentee).
        /// Admins get all.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Mentorship>>> GetMentorships()
        {
            IQueryable<Mentorship> query = _context.Mentorships
                .Include(m => m.Mentor)
                .Include(m => m.Mentee);

            if (!IsAdmin)
            {
                query = query.Where(m =>
                m.MentorId == UserId ||
                m.MenteeId == UserId);
            }

            return Ok(await query.ToListAsync());
        }

        /// <summary>
        /// Get a single mentorship by ID.
        /// </summary>
        [HttpGet("{id:int}")] // why the "int"???
        public async Task<ActionResult<Mentorship>> GetMentorship(int id) // why not IActionResult??
        {
            var mentorship = await _context.Mentorships
                .Include(m => m.Mentor)
                .Include(m => m.Mentee)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mentorship == null)
                return NotFound();

            if (!IsAdmin &&
                mentorship.MentorId != UserId &&
                mentorship.MenteeId != UserId)
            {
                return Forbid();
            }

            return Ok(mentorship);
        }

        /// <summary>
        /// Create a mentorship.
        /// Typically admin-only, but you can relax this if needed.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Mentorship>> CreateMentorship(
            [FromBody] Mentorship mentorship)
        {
            _context.Mentorships.Add(mentorship);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMentorship), // action name
                new { id = mentorship.Id }, // routeValues
                mentorship); // route
        }

        /// <summary>
        /// Update a mentorship.
        /// Mentors and mentees involved may update it.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateMentorship(
            int id,
            [FromBody] Mentorship updated)
        {
            if (id != updated.Id)
                return BadRequest();

            var mentorship = await _context.Mentorships
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mentorship == null)
                return NotFound();

            if (!IsAdmin &&
                mentorship.MentorId != UserId &&
                mentorship.MenteeId != UserId)
            {
                Forbid();
            }

            // Update allow Fields
            mentorship.Scope = updated.Scope;
            mentorship.StartDate = updated.StartDate;
            mentorship.EndDate = updated.EndDate;
            mentorship.Status = updated.Status;

            // If using rowversion
            // _context.Entry(mentorship).Property("RowVersion").OriginalValue = 
            //      updated.RowVersion;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("This mentorship was modified by another user.");
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a mentorship (admin only).
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMentorship(int id)
        {
            var mentorship = await _context.Mentorships.FindAsync(id);

            if (mentorship == null)
                return NotFound();

            _context.Mentorships.Remove(mentorship);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

}