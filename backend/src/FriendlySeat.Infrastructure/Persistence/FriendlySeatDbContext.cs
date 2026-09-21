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
    public DbSet<WaitlistPreference> WaitlistPreferences => Set<WaitlistPreference>();
    public DbSet<SeatSwapRequest> SeatSwapRequests => Set<SeatSwapRequest>();
    public DbSet<SeatSwapResponse> SeatSwapResponses => Set<SeatSwapResponse>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivitySignup> ActivitySignups => Set<ActivitySignup>();
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
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<StudyGoal> StudyGoals => Set<StudyGoal>();
    public DbSet<StudyAchievement> StudyAchievements => Set<StudyAchievement>();
    public DbSet<ReadingBook> ReadingBooks => Set<ReadingBook>();
    public DbSet<ReadingSession> ReadingSessions => Set<ReadingSession>();
    public DbSet<ReadingNote> ReadingNotes => Set<ReadingNote>();
    public DbSet<BookListShare> BookListShares => Set<BookListShare>();
    public DbSet<BookListShareFavorite> BookListShareFavorites => Set<BookListShareFavorite>();
    public DbSet<SeatNote> SeatNotes => Set<SeatNote>();
    public DbSet<ActivityComment> ActivityComments => Set<ActivityComment>();
    public DbSet<PersonaProfile> PersonaProfiles => Set<PersonaProfile>();
    public DbSet<VenuePost> VenuePosts => Set<VenuePost>();
    public DbSet<VenuePostComment> VenuePostComments => Set<VenuePostComment>();
    public DbSet<VenuePostLike> VenuePostLikes => Set<VenuePostLike>();
    public DbSet<VenuePostCommentLike> VenuePostCommentLikes => Set<VenuePostCommentLike>();

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

        modelBuilder.Entity<WaitlistPreference>()
            .HasIndex(p => new { p.VenueId, p.Status, p.CreatedAt });

        modelBuilder.Entity<WaitlistPreference>()
            .HasIndex(p => new { p.UserId, p.Status });

        modelBuilder.Entity<SeatSwapRequest>()
            .HasIndex(r => new { r.VenueId, r.Status, r.CreatedAt });

        modelBuilder.Entity<SeatSwapRequest>()
            .HasIndex(r => new { r.UserId, r.Status });

        modelBuilder.Entity<SeatSwapResponse>()
            .HasIndex(r => new { r.RequestId, r.UserId });

        modelBuilder.Entity<SeatSwapResponse>()
            .HasIndex(r => r.UserId);

        modelBuilder.Entity<Activity>()
            .HasIndex(a => new { a.Status, a.StartAt });

        modelBuilder.Entity<Activity>()
            .HasIndex(a => new { a.CreatorUserId, a.Status });

        modelBuilder.Entity<Activity>()
            .HasOne(a => a.Creator)
            .WithMany()
            .HasForeignKey(a => a.CreatorUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivitySignup>()
            .HasIndex(s => new { s.ActivityId, s.Status });

        modelBuilder.Entity<ActivitySignup>()
            .HasIndex(s => new { s.UserId, s.Status });

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

        modelBuilder.Entity<FloorPoi>()
            .HasIndex(p => p.AreaId);

        modelBuilder.Entity<Area>()
            .HasIndex(a => new { a.FloorId, a.SortOrder });

        modelBuilder.Entity<Zone>()
            .HasIndex(z => new { z.FloorId, z.OffsetX, z.OffsetY });

        modelBuilder.Entity<Zone>()
            .HasIndex(z => z.AreaId);

        modelBuilder.Entity<Advertisement>()
            .HasIndex(a => new { a.Placement, a.Status });

        modelBuilder.Entity<StudySession>()
            .HasIndex(s => new { s.UserId, s.StartedAt });

        modelBuilder.Entity<StudySession>()
            .HasIndex(s => new { s.UserId, s.Status });

        modelBuilder.Entity<StudyGoal>()
            .HasIndex(g => new { g.UserId, g.Period, g.PeriodStart });

        modelBuilder.Entity<StudyAchievement>()
            .HasIndex(a => new { a.UserId, a.Code });

        modelBuilder.Entity<ReadingBook>()
            .HasIndex(b => new { b.UserId, b.Status });

        modelBuilder.Entity<ReadingSession>()
            .HasIndex(s => new { s.UserId, s.StartedAt });

        modelBuilder.Entity<ReadingSession>()
            .HasIndex(s => new { s.UserId, s.Status });

        modelBuilder.Entity<ReadingNote>()
            .HasIndex(n => new { n.UserId, n.BookId, n.Type });

        modelBuilder.Entity<BookListShare>()
            .HasIndex(s => s.Token).IsUnique();

        modelBuilder.Entity<BookListShare>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookListShare>()
            .Property(s => s.Token).HasMaxLength(64);

        modelBuilder.Entity<BookListShareFavorite>()
            .HasIndex(f => new { f.ShareId, f.UserId }).IsUnique();

        modelBuilder.Entity<BookListShareFavorite>()
            .HasOne(f => f.Share)
            .WithMany(s => s.Favorites)
            .HasForeignKey(f => f.ShareId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookListShareFavorite>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SeatNote>()
            .HasIndex(n => new { n.SeatId, n.UserId }).IsUnique();

        modelBuilder.Entity<SeatNote>()
            .HasIndex(n => new { n.SeatId, n.Status });

        modelBuilder.Entity<SeatNote>()
            .HasOne(n => n.Seat)
            .WithMany()
            .HasForeignKey(n => n.SeatId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SeatNote>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityComment>()
            .HasIndex(c => new { c.ActivityId, c.Status });

        modelBuilder.Entity<ActivityComment>()
            .HasOne(c => c.Activity)
            .WithMany()
            .HasForeignKey(c => c.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityComment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PersonaProfile>()
            .HasIndex(p => p.UserId).IsUnique();

        modelBuilder.Entity<PersonaProfile>()
            .Property(p => p.TypeCode).HasMaxLength(64);

        modelBuilder.Entity<PersonaProfile>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VenuePost>()
            .HasIndex(p => new { p.VenueId, p.Status, p.IsPinned });

        modelBuilder.Entity<VenuePost>()
            .HasOne(p => p.Venue)
            .WithMany()
            .HasForeignKey(p => p.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VenuePost>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VenuePostComment>()
            .HasIndex(c => new { c.PostId, c.Status });

        modelBuilder.Entity<VenuePostComment>()
            .HasIndex(c => c.ParentCommentId);

        modelBuilder.Entity<VenuePostComment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VenuePostComment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VenuePostLike>()
            .HasIndex(l => new { l.PostId, l.UserId }).IsUnique();

        modelBuilder.Entity<VenuePostLike>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VenuePostLike>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VenuePostCommentLike>()
            .HasIndex(l => new { l.CommentId, l.UserId }).IsUnique();

        modelBuilder.Entity<VenuePostCommentLike>()
            .HasOne(l => l.Comment)
            .WithMany()
            .HasForeignKey(l => l.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VenuePostCommentLike>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
