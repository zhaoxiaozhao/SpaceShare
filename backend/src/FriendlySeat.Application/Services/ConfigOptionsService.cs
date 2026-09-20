using System.Text.Json;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 可配置选项（活动分类、换座原因、座位标签）：存于 SystemConfigs，ConfigKey=list，Value 为 JSON 数组。
/// 未配置时使用内置默认值；供前端只读接口与后端校验使用。
/// </summary>
public class ConfigOptionsService
{
    public const string Key = "list";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public const string DefaultActivityCategories = """
    [{"code":"reading","label":"读书"},{"code":"lecture","label":"讲座"},{"code":"exhibition","label":"展览"},{"code":"study","label":"自习"},{"code":"kaoyan","label":"考研"},{"code":"kaogong","label":"考公"},{"code":"ai","label":"AI"},{"code":"coding","label":"编程"},{"code":"sharing","label":"分享交流"},{"code":"workshop","label":"工作坊"},{"code":"film","label":"观影"},{"code":"music","label":"音乐"},{"code":"art","label":"艺术"},{"code":"sports","label":"运动"},{"code":"competition","label":"比赛"},{"code":"volunteer","label":"志愿"},{"code":"other","label":"其他"}]
    """;

    public const string DefaultSwapReasons = """
    [{"code":"light","label":"光线问题"},{"code":"cold","label":"位置偏冷"},{"code":"hot","label":"位置偏热"},{"code":"noise","label":"附近有人交谈"},{"code":"together","label":"想与同伴相邻"},{"code":"window","label":"想靠窗"},{"code":"socket","label":"需要插座"},{"code":"other","label":"其他"}]
    """;

    public const string DefaultSeatTags = """
    [{"code":"window","label":"靠窗"},{"code":"socket","label":"有插座"},{"code":"quiet","label":"安静"},{"code":"light","label":"光线好"}]
    """;

    public const string DefaultVenuePostCategories = """
    [{"code":"help","label":"求助"},{"code":"study","label":"组队自习"},{"code":"books","label":"书籍推荐"},{"code":"advice","label":"场馆建议"},{"code":"lost","label":"失物招领"},{"code":"chat","label":"闲聊"}]
    """;

    private readonly ConfigService _config;

    public ConfigOptionsService(ConfigService config)
    {
        _config = config;
    }

    public Task<List<ConfigOptionDto>> GetActivityCategoriesAsync(CancellationToken ct = default)
        => GetListAsync(ConfigCategory.ActivityCategories, DefaultActivityCategories, ct);

    public Task<List<ConfigOptionDto>> GetSwapReasonsAsync(CancellationToken ct = default)
        => GetListAsync(ConfigCategory.SwapReasons, DefaultSwapReasons, ct);

    public Task<List<ConfigOptionDto>> GetSeatTagsAsync(CancellationToken ct = default)
        => GetListAsync(ConfigCategory.SeatTags, DefaultSeatTags, ct);

    public Task<List<ConfigOptionDto>> GetVenuePostCategoriesAsync(CancellationToken ct = default)
        => GetListAsync(ConfigCategory.VenuePostCategories, DefaultVenuePostCategories, ct);

    public async Task<ConfigOptionsDto> GetAllAsync(CancellationToken ct = default)
    {
        return new ConfigOptionsDto
        {
            ActivityCategories = await GetActivityCategoriesAsync(ct),
            SwapReasons = await GetSwapReasonsAsync(ct),
            SeatTags = await GetSeatTagsAsync(ct),
            VenuePostCategories = await GetVenuePostCategoriesAsync(ct)
        };
    }

    private async Task<List<ConfigOptionDto>> GetListAsync(ConfigCategory category, string fallbackJson, CancellationToken ct)
    {
        var raw = await _config.GetValueAsync(category, Key, ct);
        var json = string.IsNullOrWhiteSpace(raw) ? fallbackJson : raw!;

        try
        {
            var list = JsonSerializer.Deserialize<List<ConfigOptionDto>>(json, JsonOpts);
            if (list is { Count: > 0 }) return list.Where(x => !string.IsNullOrWhiteSpace(x.Code)).ToList();
        }
        catch (JsonException)
        {
            // 配置格式错误时回退默认
        }

        return JsonSerializer.Deserialize<List<ConfigOptionDto>>(fallbackJson, JsonOpts) ?? new List<ConfigOptionDto>();
    }
}
