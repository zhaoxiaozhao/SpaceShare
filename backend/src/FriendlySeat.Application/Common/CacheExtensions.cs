namespace FriendlySeat.Application.Common;

// 座位数据变更后，需要失效的缓存键统一在此管理
public static class CacheKeys
{
    public const string VenuePrefix = "venue:";
    public const string SeatPrefix = "seat:";

    public static string Venue(long venueId) => $"{VenuePrefix}{venueId}";
    public static string Seat(long seatId) => $"{SeatPrefix}{seatId}";
}
