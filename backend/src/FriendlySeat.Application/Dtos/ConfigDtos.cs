namespace FriendlySeat.Application.Dtos;

public class ConfigOptionDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class ConfigOptionsDto
{
    public List<ConfigOptionDto> ActivityCategories { get; set; } = new();
    public List<ConfigOptionDto> SwapReasons { get; set; } = new();
    public List<ConfigOptionDto> SeatTags { get; set; } = new();
}
