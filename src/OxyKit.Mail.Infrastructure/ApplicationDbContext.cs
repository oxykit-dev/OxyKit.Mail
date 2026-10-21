using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Infrastructure;

/// <summary>
/// Application database context for OxyKit.Mail.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public ApplicationDbContext() { }

    // Domain entities (from OxyKit.Mail.Infrastructure)
    public DbSet<Message> Messages { get; set; }
    public DbSet<Attachment> Attachments { get; set; }

    // Persistence entities
    public DbSet<MessageLog> MessageLogs { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Message-Attachment relationship
        builder
            .Entity<Attachment>()
            .HasOne(ea => ea.Message)
            .WithMany(em => em.Attachments)
            .HasForeignKey(ea => ea.MessageId);

        // Configure MessageLog-Message relationship
        builder
            .Entity<MessageLog>()
            .HasOne(el => el.Message)
            .WithMany()
            .HasForeignKey(el => el.MessageId);

        // Configure ApiKey-User relationship
        builder
            .Entity<ApiKey>()
            .HasOne(k => k.User)
            .WithMany(u => u.ApiKeys)
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure RefreshToken-User relationship
        builder
            .Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
