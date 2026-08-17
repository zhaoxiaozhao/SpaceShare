using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Infrastructure.Persistence;

public class FriendlySeatDbContext : DbContext, IAppDbContext
{
    public FriendlySeatDbContext(DbContextOptions<FriendlySeatDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserContact> UserContacts => Set<UserContact>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<FloorPoi> FloorPois => Set<FloorPoi>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<SeatSession> SeatSessions => Set<SeatSession>();
    public DbSet<SeatShare> SeatShares => Set<SeatShare>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationWaitlist> ReservationWaitlists => Set<ReservationWaitlist>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<RiskEvent> RiskEvents => Set<RiskEvent>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Advertiser> Advertisers => Set<Advertiser>();
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<AdImpression> AdImpressions => Set<AdImpression>();
    public DbSet<AdClick> AdClicks => Set<AdClick>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PublicContribution> PublicContributions => Set<PublicContribution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FriendlySeatDbContext).Assembly);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.OpenId).IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.CreditScore).HasDefaultValue(100);

        modelBuilder.Entity<User>()
            .Property(u => u.RiskScore).HasDefaultValue(0);

        modelBuilder.Entity<City>()
            .HasIndex(c => new { c.CountryCode, c.Name }).IsUnique();

        modelBuilder.Entity<Venue>()
            .HasIndex(v => new { v.CityId, v.Name });

        modelBuilder.Entity<Venue>()
            .HasIndex(v => new { v.Longitude, v.Latitude });

        modelBuilder.Entity<Seat>()
            .HasIndex(s => new { s.ZoneId, s.Code });

        modelBuilder.Entity<SeatShare>()
            .HasIndex(s => new { s.SeatId, s.StartAt, s.EndAt });

        modelBuilder.Entity<SeatShare>()
            .HasIndex(s => new { s.SeatId, s.Status });

        modelBuilder.Entity<SeatShare>()
            .HasIndex(s => s.SourceSessionId);

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.SeatId, r.StartAt, r.EndAt });

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.SeatId, r.Status });

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => new { r.UserId, r.Status });

        modelBuilder.Entity<ReservationWaitlist>()
            .HasIndex(w => new { w.ShareId, w.Status });

        modelBuilder.Entity<ReservationWaitlist>()
            .HasIndex(w => new { w.UserId, w.Status });

        modelBuilder.Entity<UserContact>()
            .HasIndex(c => new { c.UserId, c.ContactType });

        modelBuilder.Entity<Report>()
            .HasIndex(r => new { r.TargetType, r.TargetId });

        modelBuilder.Entity<Report>()
            .HasIndex(r => r.Status);

        modelBuilder.Entity<AdminUser>()
            .HasIndex(a => a.Username).IsUnique();

        modelBuilder.Entity<SystemConfig>()
            .HasIndex(c => new { c.Category, c.ConfigKey }).IsUnique();

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        modelBuilder.Entity<CreditTransaction>()
            .HasIndex(t => new { t.UserId, t.CreatedAt });

        modelBuilder.Entity<RiskEvent>()
            .HasIndex(e => new { e.UserId, e.CreatedAt });

        modelBuilder.Entity<SeatSession>()
            .HasIndex(s => new { s.SeatId, s.Status });

        modelBuilder.Entity<SeatSession>()
            .HasIndex(s => new { s.UserId, s.Status });

        modelBuilder.Entity<PublicContribution>()
            .HasIndex(c => c.UserId).IsUnique();

        modelBuilder.Entity<FloorPoi>()
            .HasIndex(p => new { p.FloorId, p.Type });

        modelBuilder.Entity<Area>()
            .HasIndex(a => new { a.FloorId, a.SortOrder });

        modelBuilder.Entity<Zone>()
            .HasIndex(z => new { z.FloorId, z.OffsetX, z.OffsetY });

        modelBuilder.Entity<Zone>()
            .HasIndex(z => z.AreaId);

        modelBuilder.Entity<Advertisement>()
            .HasIndex(a => new { a.Placement, a.Status });
    }
}
