using Gchain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gchain.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<GameSession> GameSessions { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<WordGuess> WordGuesses { get; set; }
    public DbSet<RoundResult> RoundResults { get; set; }
    public DbSet<TeamChatMessage> TeamChatMessages { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // UserSession configuration
        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RefreshToken);

            entity
                .HasOne(us => us.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GameSession configuration
        builder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Language).HasConversion<string>();
        });

        // Team configuration
        builder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.GameSessionId);

            entity
                .HasOne(t => t.GameSession)
                .WithMany(gs => gs.Teams)
                .HasForeignKey(t => t.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TeamMember configuration
        builder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.TeamId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.TeamId);
            entity.HasIndex(e => e.UserId);

            entity
                .HasOne(tm => tm.Team)
                .WithMany(t => t.TeamMembers)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(tm => tm.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WordGuess configuration
        builder.Entity<WordGuess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.GameSessionId, e.RoundNumber });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.GameSessionId);

            entity
                .HasOne(wg => wg.GameSession)
                .WithMany(gs => gs.WordGuesses)
                .HasForeignKey(wg => wg.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(wg => wg.User)
                .WithMany(u => u.WordGuesses)
                .HasForeignKey(wg => wg.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(wg => wg.Team)
                .WithMany(t => t.WordGuesses)
                .HasForeignKey(wg => wg.TeamId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // RoundResult configuration
        builder.Entity<RoundResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.GameSessionId, e.RoundNumber }).IsUnique();
            entity.HasIndex(e => e.GameSessionId);
            entity.HasIndex(e => e.WinningTeamId);

            entity
                .HasOne(rr => rr.GameSession)
                .WithMany(gs => gs.RoundResults)
                .HasForeignKey(rr => rr.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(rr => rr.WinningTeam)
                .WithMany(t => t.WonRounds)
                .HasForeignKey(rr => rr.WinningTeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TeamChatMessage configuration
        builder.Entity<TeamChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.TeamId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);

            entity
                .HasOne(tcm => tcm.Team)
                .WithMany(t => t.ChatMessages)
                .HasForeignKey(tcm => tcm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(tcm => tcm.User)
                .WithMany(u => u.ChatMessages)
                .HasForeignKey(tcm => tcm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Badge configuration
        builder.Entity<Badge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        // UserBadge configuration (many-to-many)
        builder.Entity<UserBadge>(entity =>
        {
            entity.HasKey(ub => new { ub.UserId, ub.BadgeId });
            entity.HasIndex(ub => ub.UserId);
            entity.HasIndex(ub => ub.BadgeId);

            entity
                .HasOne(ub => ub.User)
                .WithMany(u => u.UserBadges)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(ub => ub.Badge)
                .WithMany(b => b.UserBadges)
                .HasForeignKey(ub => ub.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsRead);

            entity
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // User configuration
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
