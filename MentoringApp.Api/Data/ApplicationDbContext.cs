using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Data
{
	public class ApplicationDbContext : IdentityDbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
						: base(options) { }

		public DbSet<Profile> Profiles { get; set; }
		public DbSet<Skill> Skills { get; set; }
		public DbSet<UserSkill> UserSkills { get; set; }
		public DbSet<SkillCategory> SkillCategories { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<Mentorship> Mentorships { get; set; }
		public DbSet<Testimonial> Testimonials { get; set; }
		public DbSet<RefreshToken> RefreshTokens { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder); // required for IdentityDbContext

			// -------------------------------------
			// PROFILE (1:1 ApplicationUser → Profile)
			// -------------------------------------
			modelBuilder.Entity<Profile>(entity =>
			{
				entity.HasKey(p => p.Id);

				entity.HasOne(p => p.User)
					.WithOne(u => u.Profile)
					.HasForeignKey<Profile>(p => p.UserId)
					.OnDelete(DeleteBehavior.Cascade); // disable only
			});

			// -------------------------------------
			// SKILLS
			// -------------------------------------
			modelBuilder.Entity<Skill>(entity =>
			{
				entity.HasKey(s => s.Id);

				entity.HasIndex(s => s.NormalizedName)
					.IsUnique();

				entity.Property(s => s.Name)
					.IsRequired()
					.HasMaxLength(150);

				entity.HasIndex(s => s.Name)
					.IsUnique();

				entity.Property(s => s.Status)
					.HasConversion<int>();

				entity.HasOne(s => s.DuplicateOfSkill)
					.WithMany()
					.HasForeignKey(s => s.DuplicateOfSkillId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			// -------------------------------------
			// SKILL CATEGORIES
			// -------------------------------------
			modelBuilder.Entity<SkillCategory>(entity =>
			{
				entity.HasKey(c => c.Id);

				entity.Property(c => c.Name)
				.IsRequired()
				.HasMaxLength(100);

				entity.HasIndex(c => c.Name)
					.IsUnique();
			});

			modelBuilder.Entity<Skill>()
				.HasMany(s => s.Categories)
				.WithMany(c => c.Skills)
				.UsingEntity(j => j.ToTable("SkillCategoryMappings"));

			// -------------------------------------
			// TAGS
			// -------------------------------------
			modelBuilder.Entity<Tag>(entity =>
			{
				entity.HasKey(t => t.Id);

				entity.Property(t => t.Name)
					.IsRequired()
					.HasMaxLength(50);

				entity.HasIndex(t => t.Name)
					.IsUnique();

			});

			// -------------------------------------
			// USER SKILLS (many-to-many via bridge)
			// -------------------------------------
			modelBuilder.Entity<UserSkill>(entity =>
			{
				entity.HasKey(us => us.Id);

				entity.HasOne(us => us.User)
					.WithMany(u => u.UserSkills)
					.HasForeignKey(us => us.UserId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(us => us.Skill)
					.WithMany(s => s.UserSkills)
					.HasForeignKey(us => us.SkillId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// -------------------------------------
			// MENTORSHIPS (User → Mentor/Mentee)
			// -------------------------------------
			modelBuilder.Entity<Mentorship>(entity =>
			{
				entity.HasKey(m => m.Id);

				entity.HasOne(m => m.Mentor)
					.WithMany(u => u.MentorshipsAsMentor)
					.HasForeignKey(m => m.MentorId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(m => m.Mentee)
					.WithMany(u => u.MentorshipAsMentee)
					.HasForeignKey(m => m.MenteeId)
					.OnDelete(DeleteBehavior.Restrict);

				// concurrency -- not included in sqlite test db
				if (Database.IsNpgsql())
				{
					entity.Property<uint>("xmin")
						.HasColumnName("xmin")
						.HasColumnType("xid")
						.IsRowVersion();
				}
			});

			// -------------------------------------
			// TESTIMONIALS
			// -------------------------------------
			modelBuilder.Entity<Testimonial>(entity =>
			{
				entity.HasKey(t => t.Id);

				entity.HasOne(t => t.Recipient)
					.WithMany(u => u.ReceivedTestimonials)
					.HasForeignKey(t => t.RecipientId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(t => t.Author)
					.WithMany(u => u.WrittenTestimonials)
					.HasForeignKey(t => t.AuthorId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(t => t.Mentorship)
					.WithMany(m => m.Testimonials)
					.HasForeignKey(t => t.MentorshipId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// RefreshToken basic mapping (optional)
			modelBuilder.Entity<RefreshToken>(entity =>
			{
				entity.HasKey(rt => rt.Id);
				entity.Property(rt => rt.Token).IsRequired();
				entity.Property(rt => rt.UserId).IsRequired();
			});
		}
	}
}

