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

public enum WaitlistPreferenceStatus
{
    Active,
    Booked,
    Cancelled,
    Expired
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
    Review,
    Feedback,
    Activity
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
    NotificationTemplates,
    ActivityCategories,
    SwapReasons,
    SeatTags
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

public enum BookStatus
{
    WantToRead,
    Reading,
    Finished
}

public enum ReadingSessionStatus
{
    Active,
    Completed
}

public enum ReadingNoteType
{
    Highlight,
    Note
}

public enum SeatSwapStatus
{
    Open,
    Matched,
    Cancelled,
    Expired
}

public enum SeatSwapResponseStatus
{
    Pending,
    Accepted,
    Rejected
}

public enum ActivityStatus
{
    PendingReview,
    Published,
    Rejected,
    Cancelled,
    Finished
}

public enum ActivitySignupStatus
{
    Joined,
    Cancelled
}
