using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Mentorship;
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

        /// <summary>
        /// Get mentorships for the current user (mentor or mentee).
        /// Admins get all.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MentorshipDto>>> GetMentorships()
        {
            IQueryable<Mentorship> query = _db.Mentorships
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
        [HttpGet("{id:int}")] 
        public async Task<ActionResult<MentorshipDto>> GetMentorship(int id) // why not IActionResult??
        {
            var mentorship = await _db.Mentorships
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
        public async Task<ActionResult<MentorshipDto>> CreateMentorship(
            [FromBody] MentorshipDto dto)
        {
            var entity = new Mentorship
            {
                MentorId = dto.MentorId,
                MenteeId = dto.MenteeId,
                Scope = dto.Scope,
                Status = dto.Status,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
                // ❌ Do NOT set navigation properties
                // ❌ Do NOT set RowVersion
            };


            _db.Mentorships.Add(entity);
            await _db.SaveChangesAsync();

            dto.Id = entity.Id;

            return CreatedAtAction(
                nameof(GetMentorship), // action name
                new { id = entity.Id }, // routeValues
                dto); // route
        }

        /// <summary>
        /// Update a mentorship.
        /// Mentors and mentees involved may update it.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateMentorship(
            int id,
            [FromBody] MentorshipDto updated)
        {
            if (id != updated.Id)
                return BadRequest();

            var mentorship = await _db.Mentorships
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

            // row version
            if (_db.Database.IsNpgsql())
            {
                _db.Entry(mentorship)
                    .Property("xmin")
                    .OriginalValue = BitConverter.ToUInt32(updated.Version);
            }

            // TODO: Add automatic retry / merge strategies

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

        /// <summary>
        /// Delete a mentorship (admin only).
        /// </summary>
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
    }

}