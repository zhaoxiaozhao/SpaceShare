namespace FriendlySeat.Application.Dtos;

public class SeatNoteDto
{
    public long Id { get; set; }
    public long SeatId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerAvatar { get; set; }

    /// <summary>是否当前查看者本人（用于显示编辑/删除）</summary>
    public bool IsOwner { get; set; }

    public string Status { get; set; } = "Visible";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>管理端展示：座位编号</summary>
    public string? SeatCode { get; set; }
}

public class CreateSeatNoteRequest
{
    public long SeatId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class AdminSeatNoteReviewRequest
{
    /// <summary>true=审核通过（恢复展示）；false=驳回（删除）</summary>
    public bool Approve { get; set; }
}
