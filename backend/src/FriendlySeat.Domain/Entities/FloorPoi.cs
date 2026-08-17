namespace FriendlySeat.Domain.Entities;

public enum PoiType
{
    Toilet,
    DrinkingWater,
    Bookshelf,
    Elevator,
    Stairs,
    Corridor,
    Entrance,
    Exit,
    ServiceDesk,
    Other,
    Text,
    Line
}

/// <summary>楼层平面图标志物：卫生间、饮水机、书架、电梯口、楼梯、走廊、入口等</summary>
public class FloorPoi
{
    public long Id { get; set; }
    public long FloorId { get; set; }
    public PoiType Type { get; set; } = PoiType.Other;
    public string Name { get; set; } = string.Empty;

    /// <summary>楼层网格坐标 X（与 Zone 网格同一坐标系）</summary>
    public int PositionX { get; set; }

    /// <summary>楼层网格坐标 Y</summary>
    public int PositionY { get; set; }

    /// <summary>占用宽度（跨网格格数，用于书架/走廊等面状标志物）</summary>
    public int Width { get; set; } = 1;

    /// <summary>占用高度（跨网格格数）</summary>
    public int Height { get; set; } = 1;

    /// <summary>朝向说明（如 东/西/北入口）</summary>
    public string? Direction { get; set; }

    /// <summary>旋转角度（0-360），用于文本/线条的方向调整</summary>
    public int Rotation { get; set; }

    /// <summary>文本内容（Text 类型专用）</summary>
    public string? Text { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Floor? Floor { get; set; }
}
