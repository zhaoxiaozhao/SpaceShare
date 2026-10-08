namespace FriendlySeat.Application.Dtos;

/// <summary>实时在线（正在学习/阅读）人数，聚合匿名统计</summary>
public class LiveStatsDto
{
    /// <summary>正在自习人数</summary>
    public int Studying { get; set; }

    /// <summary>正在阅读人数</summary>
    public int Reading { get; set; }

    /// <summary>去重后的总人数（正在自习或阅读）</summary>
    public int Total { get; set; }
}
