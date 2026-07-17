using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebChat.Entities;
using WebChat.Models;

namespace WebChat.DAL
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Conversation> Conversations { get; set; } = default!;
        public DbSet<Message> Messages { get; set; } = default!;
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RequireUniqueEmail is a check-then-insert in UserManager, so two
            // concurrent registrations can still slip past it. This is the
            // actual guarantee. Filtered like Identity's own UserNameIndex, so
            // rows without an email don't collide with each other on NULL.
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL");

            modelBuilder.Entity<Conversation>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConversationParticipant>()
                .HasOne(p => p.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(p => p.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConversationParticipant>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One row per person per conversation — the get-or-create DM lookup
            // relies on this, and it stops a double-click adding you twice.
            modelBuilder.Entity<ConversationParticipant>()
                .HasIndex(p => new { p.ConversationId, p.UserId })
                .IsUnique();
        }
    }
}
