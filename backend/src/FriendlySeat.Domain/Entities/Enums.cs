namespace FriendlySeat.Domain.Entities;

public enum UserStatus
{
    Active = 1,
    Banned = 2,
    Deleted = 3
}

public enum VenueType
{
    Library,
    UniversityLibrary,
    ReadingRoom,
    StudySpace,
    Coworking,
    CommunitySpace
}

public enum EntityStatus
{
    Active = 1,
    Disabled = 2
}

public enum SeatType
{
    Normal,
    WindowSeat,
    ComputerSeat,
    Lounge
}

public enum SeatStatus
{
    Available,
    Occupied,
    Unavailable
}

public enum SeatSessionStatus
{
    Pending,
    Active,
    Completed,
    Expired,
    Cancelled
}

public enum SeatShareStatus
{
    Available,
    Reserved,
    Active,
    Completed,
    Cancelled,
    Expired
}

public enum ReservationStatus
{
    Reserved,
    Arrived,
    Using,
    Completed,
    Cancelled,
    NoShow,
    Expired
}

public enum WaitlistStatus
{
    Waiting,
    Notified,
    Reserved,
    Expired,
    Cancelled
}

public enum ContactType
{
    WechatId,
    WechatQrCode,
    Phone,
    Other
}

public enum ReportTargetType
{
    Seat,
    Share,
    Reservation,
    User,
    Review
}

public enum ReportStatus
{
    Pending,
    Ignored,
    Warned,
    CreditDeducted,
    ReservationCancelled,
    AccountRestricted,
    Banned,
    Resolved
}

public enum DonationStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum AdStatus
{
    Active,
    Paused,
    Expired
}

public enum AdminRole
{
    SuperAdmin,
    Admin,
    Moderator,
    AdManager,
    MerchantManager
}

public enum NotificationType
{
    ReservationCreated,
    ReservationStarting,
    ArrivalRequired,
    ReservationExpired,
    ReservationCancelled,
    WaitlistAvailable,
    CreditChanged,
    ReportResult,
    System
}

public enum ConfigCategory
{
    ReservationRules,
    CreditRules,
    RiskRules,
    ArrivalRules,
    ImageRules,
    SensitiveWords,
    NotificationTemplates
}

public enum StudyType
{
    Reading,
    Programming,
    English,
    Exam,
    Postgraduate,
    Papers,
    Other
}

public enum StudySessionStatus
{
    Active,
    Completed,
    Cancelled
}

public enum GoalPeriod
{
    Daily,
    Weekly,
    Monthly
}
