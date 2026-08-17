namespace FriendlySeat.Domain.Entities;

/// <summary>
/// 楼层内的空间区域（如 主空间、走廊区域、平台区域），一个楼层可包含多个区域，
/// 每个区域包含多个座位区块（Zone）。
/// </summary>
public class Area
{
    public long Id { get; set; }
    public long FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Floor? Floor { get; set; }
    public ICollection<Zone> Zones { get; set; } = new List<Zone>();
}
