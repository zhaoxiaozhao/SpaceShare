using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FriendlySeat.Application.Common;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserContact> UserContacts { get; }
    DbSet<City> Cities { get; }
    DbSet<Venue> Venues { get; }
    DbSet<Building> Buildings { get; }
    DbSet<Floor> Floors { get; }
    DbSet<FloorPoi> FloorPois { get; }
    DbSet<Area> Areas { get; }
    DbSet<Zone> Zones { get; }
    DbSet<Seat> Seats { get; }
    DbSet<SeatSession> SeatSessions { get; }
    DbSet<SeatShare> SeatShares { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<ReservationWaitlist> ReservationWaitlists { get; }
    DbSet<CreditTransaction> CreditTransactions { get; }
    DbSet<RiskEvent> RiskEvents { get; }
    DbSet<Report> Reports { get; }
    DbSet<Donation> Donations { get; }
    DbSet<Advertiser> Advertisers { get; }
    DbSet<Advertisement> Advertisements { get; }
    DbSet<AdImpression> AdImpressions { get; }
    DbSet<AdClick> AdClicks { get; }
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<AdminAuditLog> AdminAuditLogs { get; }
    DbSet<SystemConfig> SystemConfigs { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<PublicContribution> PublicContributions { get; }
    DbSet<StudySession> StudySessions { get; }
    DbSet<StudyGoal> StudyGoals { get; }
    DbSet<StudyAchievement> StudyAchievements { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
