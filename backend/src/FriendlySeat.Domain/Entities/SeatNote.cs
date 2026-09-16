namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 座位便签：用户在某座位上留的一句话（≤50 字），展示日期时间；每人每座位一条，可修改/删除。
/// 被举报后自动隐藏（Hidden）并进入后台审核；平台仅提供信息展示，禁止联系方式/交易等信息。
/// </summary>
public class SeatNote
{
    public long Id { get; set; }
    public long SeatId { get; set; }
    public long UserId { get; set; }

    /// <summary>便签内容（≤50 字）</summary>
    public string Content { get; set; } = string.Empty;

    public SeatNoteStatus Status { get; set; } = SeatNoteStatus.Visible;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Seat? Seat { get; set; }
    public User? User { get; set; }
}
