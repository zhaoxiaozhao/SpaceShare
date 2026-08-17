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
