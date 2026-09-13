namespace FriendlySeat.Domain.Entities;

public class WaitlistPreference
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long VenueId { get; set; }

    /// <summary>限定楼层（null = 不限楼层）</summary>
    public long? FloorId { get; set; }

    /// <summary>限定区域（null = 不限区域）</summary>
    public long? AreaId { get; set; }

    /// <summary>候补偏好：none / window / socket / quiet</summary>
    public string Preference { get; set; } = "none";

    /// <summary>候补截止时间（UTC）；超过后自动失效，不再自动预约</summary>
    public DateTime? ExpireAt { get; set; }

    public WaitlistPreferenceStatus Status { get; set; } = WaitlistPreferenceStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? BookedAt { get; set; }

    /// <summary>自动预约成功后关联的预约 Id</summary>
    public long? ReservationId { get; set; }

    public User? User { get; set; }
    public Venue? Venue { get; set; }
    public Floor? Floor { get; set; }
    public Area? Area { get; set; }
    public Reservation? Reservation { get; set; }
}