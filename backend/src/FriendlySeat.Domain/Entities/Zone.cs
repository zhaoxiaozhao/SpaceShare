namespace FriendlySeat.Domain.Entities;

public class Zone
{
    public long Id { get; set; }
    public long FloorId { get; set; }
    public long? AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? MapImageUrl { get; set; }

    /// <summary>区块网格行数，用于座位排布图（0 表示按座位数量自适应）</summary>
    public int GridRows { get; set; }

    /// <summary>区块网格列数，用于座位排布图（0 表示按座位数量自适应）</summary>
    public int GridCols { get; set; }

    /// <summary>区块在楼层网格中的水平偏移（用于多区块拼装楼层平面图）</summary>
    public int OffsetX { get; set; }

    /// <summary>区块在楼层网格中的垂直偏移（用于多区块拼装楼层平面图）</summary>
    public int OffsetY { get; set; }

    public Floor? Floor { get; set; }
    public Area? Area { get; set; }
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
