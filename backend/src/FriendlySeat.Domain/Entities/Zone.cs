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

    /// <summary>布局模式：grid=普通网格 / table=桌椅格局 / arc=弧形 / ellipse=椭圆弧 / spiral=螺旋 / sine=S形正弦波 / slant=斜线排布 / curve=自定义曲线</summary>
    public string LayoutMode { get; set; } = "grid";

    // ---- table 模式参数 ----
    /// <summary>每桌横向座位数（如 2x2、2x3 格局中的 2）</summary>
    public int TableSeatCols { get; set; } = 2;

    /// <summary>每桌纵向座位数</summary>
    public int TableSeatRows { get; set; } = 2;

    /// <summary>桌与桌之间的横向过道（格数，支持 0.5 步进）</summary>
    public double TableGapX { get; set; } = 1;

    /// <summary>桌与桌之间的纵向过道（格数，支持 0.5 步进）</summary>
    public double TableGapY { get; set; } = 1;

    /// <summary>横向桌子数量</summary>
    public int TablesX { get; set; } = 1;

    /// <summary>纵向桌子数量</summary>
    public int TablesY { get; set; } = 1;

    // ---- arc 模式参数 ----
    /// <summary>内圈半径（格数，圆心到第一圈的距离）</summary>
    public double ArcRadius { get; set; } = 8;

    /// <summary>圈间距（格数）</summary>
    public double ArcRadiusStep { get; set; } = 1.5;

    /// <summary>起始角度（度，屏幕坐标系，180=左 270=上 0=右）</summary>
    public double ArcStartAngle { get; set; } = 180;

    /// <summary>结束角度（度）</summary>
    public double ArcEndAngle { get; set; } = 360;

    /// <summary>弧形圈数（排数）</summary>
    public int ArcRows { get; set; } = 3;

    /// <summary>每圈座位数</summary>
    public int ArcSeatsPerRow { get; set; } = 8;

    // ---- 椭圆弧（ellipse）参数 ----
    /// <summary>短半轴（格数），与 ArcRadius（长半轴）决定椭圆形状</summary>
    public double ArcAxisB { get; set; } = 8;

    // ---- S形正弦波（sine）参数 ----
    /// <summary>振幅（格数）</summary>
    public double CurveAmplitude { get; set; } = 2;

    /// <summary>波长（格数，一个完整周期的跨度）</summary>
    public double CurveWavelength { get; set; } = 6;

    /// <summary>相位偏移（度）</summary>
    public double CurvePhase { get; set; } = 0;

    /// <summary>正弦波行间距（格数，纵向排布时相邻行的垂直距离）</summary>
    public double CurveRowGap { get; set; } = 2;

    // ---- 斜线排布（slant）参数 ----
    /// <summary>倾角（度，0-90，相对水平方向）</summary>
    public double CurveAngle { get; set; } = 30;

    /// <summary>斜线行间距（格数，垂直方向）</summary>
    public double CurveSlantGap { get; set; } = 2;

    // ---- 自定义曲线（curve）参数 ----
    /// <summary>曲线控制点 JSON：[{"x":0.5,"y":0.5},...]，区块本地网格坐标（中心语义），用于手绘不规则排布</summary>
    public string? PathPoints { get; set; }

    public Floor? Floor { get; set; }
    public Area? Area { get; set; }
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
