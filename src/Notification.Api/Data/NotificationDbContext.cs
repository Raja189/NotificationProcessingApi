using Microsoft.EntityFrameworkCore;
using Notification.Api.Models;

namespace Notification.Api.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Models.Notification> Notifications
    => Set<Models.Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Models.Notification>();
        entity.HasKey(n => n.Id);
        entity.Property(n => n.CustomerId).HasMaxLength(100).IsRequired();
        entity.Property(n => n.Recipient).HasMaxLength(320).IsRequired();
        entity.Property(n => n.Message).HasMaxLength(10000).IsRequired();
        entity.Property(n => n.RequestFingerprint).HasMaxLength(64).IsRequired();
        entity.HasIndex(n => new { n.CustomerId, n.CreatedAt });
        entity.HasIndex(n => new { n.Status, n.RequestedDeliveryTime });
        entity.HasIndex(n => new { n.RequestFingerprint, n.CreatedAt });
    }
}
