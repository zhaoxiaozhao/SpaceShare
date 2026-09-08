namespace FriendlySeat.Application.Dtos;

public class CityDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "CN";
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
}

public class VenueListItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string OpeningTime { get; set; } = string.Empty;
    public string ClosingTime { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public int AvailableCount { get; set; }
    public double? DistanceKm { get; set; }

    /// <summary>状态（Active=显示 Disabled=隐藏），管理端用</summary>
    public string? Status { get; set; }
}

public class VenueDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? Description { get; set; }
    public string OpeningTime { get; set; } = string.Empty;
    public string ClosingTime { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public int AvailableCount { get; set; }
    public List<FloorDto> Floors { get; set; } = new();
}

public class FloorDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? MapImageUrl { get; set; }
    public List<AreaDto> Areas { get; set; } = new();
    public List<ZoneDto> Zones { get; set; } = new();
    public List<PoiDto> Pois { get; set; } = new();
}

public class AreaDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PoiDto
{
    public long Id { get; set; }
    public long? AreaId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public string? Direction { get; set; }
    public int Rotation { get; set; }
    public string? Text { get; set; }
}

public class ZoneDto
{
    public long Id { get; set; }
    public long? AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? MapImageUrl { get; set; }
    public int GridRows { get; set; }
    public int GridCols { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    /// <summary>布局模式：grid=普通网格 / table=桌椅格局 / arc=弧形 / ellipse=椭圆弧 / spiral=螺旋 / sine=S形正弦波 / slant=斜线排布 / curve=自定义曲线</summary>
    public string LayoutMode { get; set; } = "grid";

    /// <summary>每桌横向座位数（table 模式）</summary>
    public int TableSeatCols { get; set; } = 2;

    /// <summary>每桌纵向座位数（table 模式）</summary>
    public int TableSeatRows { get; set; } = 2;

    /// <summary>桌间横向过道（table 模式，格数，支持 0.5 步进）</summary>
    public double TableGapX { get; set; } = 1;

    /// <summary>桌间纵向过道（table 模式，格数，支持 0.5 步进）</summary>
    public double TableGapY { get; set; } = 1;

    /// <summary>横向桌子数量（table 模式）</summary>
    public int TablesX { get; set; } = 1;

    /// <summary>纵向桌子数量（table 模式）</summary>
    public int TablesY { get; set; } = 1;

    /// <summary>内圈半径（arc 模式，格数）</summary>
    public double ArcRadius { get; set; } = 8;

    /// <summary>圈间距（arc 模式，格数）</summary>
    public double ArcRadiusStep { get; set; } = 1.5;

    /// <summary>起始角度（arc 模式，度）</summary>
    public double ArcStartAngle { get; set; } = 180;

    /// <summary>结束角度（arc 模式，度）</summary>
    public double ArcEndAngle { get; set; } = 360;

    /// <summary>弧形圈数（arc 模式）</summary>
    public int ArcRows { get; set; } = 3;

    /// <summary>每圈座位数（arc 模式）</summary>
    public int ArcSeatsPerRow { get; set; } = 8;

    /// <summary>椭圆短半轴（ellipse 模式，格数）</summary>
    public double ArcAxisB { get; set; } = 8;

    /// <summary>S形正弦波振幅（sine 模式，格数）</summary>
    public double CurveAmplitude { get; set; } = 2;

    /// <summary>S形正弦波波长（sine 模式，格数）</summary>
    public double CurveWavelength { get; set; } = 6;

    /// <summary>S形正弦波相位（sine 模式，度）</summary>
    public double CurvePhase { get; set; } = 0;

    /// <summary>S形正弦波行间距（sine 模式，格数）</summary>
    public double CurveRowGap { get; set; } = 2;

    /// <summary>斜线倾角（slant 模式，度 -90~90，负值反向）</summary>
    public double CurveAngle { get; set; } = 30;

    /// <summary>斜线行间距（slant 模式，格数）</summary>
    public double CurveSlantGap { get; set; } = 2;

    /// <summary>自定义曲线控制点 JSON（curve 模式）：[{"x":0.5,"y":0.5},...]</summary>
    public string? PathPoints { get; set; }

    public List<SeatDto> Seats { get; set; } = new();
}

public class SeatDto
{
    public long Id { get; set; }
    public long ZoneId { get; set; }
    public string Code { get; set; } = string.Empty;

    /// <summary>展示编号（B区-002）</summary>
    public string DisplayCode { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public bool Window { get; set; }
    public bool PowerSocket { get; set; }
    public int? QuietLevel { get; set; }
    public int? LightLevel { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Description { get; set; }
    public bool Verified { get; set; }
    public int CurrentReservedCount { get; set; }
    public int CurrentShareCount { get; set; }

    /// <summary>所属场馆名称</summary>
    public string VenueName { get; set; } = string.Empty;

    /// <summary>所属楼层（如 3F）</summary>
    public string FloorName { get; set; } = string.Empty;

    /// <summary>所属空间区域名（如 主空间）</summary>
    public string? AreaName { get; set; }

    /// <summary>所属场馆闭馆时间（HH:mm）</summary>
    public string ClosingTime { get; set; } = "22:00";
}
