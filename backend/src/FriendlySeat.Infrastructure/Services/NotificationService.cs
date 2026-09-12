using FriendlySeat.Application.Common;
using FriendlySeat.Application.Services;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace FriendlySeat.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<NotificationService> _logger;
    private readonly IWechatService _wechat;
    private readonly ConfigService _config;

    // 通知类型 → 订阅消息模板配置 key（SystemConfigs 中 NotificationTemplates 分类下配置）
    private static readonly Dictionary<NotificationType, string> TemplateKeys = new()
    {
        [NotificationType.ReservationCreated] = "reservation_created",
        [NotificationType.ReservationStarting] = "reservation_starting",
        [NotificationType.ArrivalRequired] = "arrival_required",
        [NotificationType.ReservationExpired] = "reservation_expired",
        [NotificationType.ReservationCancelled] = "reservation_cancelled",
        [NotificationType.WaitlistAvailable] = "waitlist_available",
        [NotificationType.CreditChanged] = "credit_changed",
        [NotificationType.ReportResult] = "report_result",
        [NotificationType.System] = "system"
    };

    public NotificationService(IAppDbContext db, ILogger<NotificationService> logger, IWechatService wechat, ConfigService config)
    {
        _db = db;
        _logger = logger;
        _wechat = wechat;
        _config = config;
    }

    public async Task SendAsync(long userId, NotificationType type, string title, string? content = null, string? data = null, CancellationToken ct = default)
    {
        try
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Content = content,
                Data = data,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送通知失败 userId={UserId} type={Type}", userId, type);
        }

        // 微信订阅消息推送（未配置模板时静默跳过，不影响主流程）
        try
        {
            await PushSubscribeMessageAsync(userId, type, title, content, data, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "订阅消息推送失败 userId={UserId} type={Type}", userId, type);
        }
    }

    private async Task PushSubscribeMessageAsync(long userId, NotificationType type, string title, string? content, string? data, CancellationToken ct)
    {
        if (!TemplateKeys.TryGetValue(type, out var key)) return;

        var templateId = await _config.GetValueAsync(ConfigCategory.NotificationTemplates, key, ct);
        if (string.IsNullOrEmpty(templateId)) return;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || string.IsNullOrEmpty(user.OpenId)) return;

        var page = type switch
        {
            NotificationType.ReservationCreated or NotificationType.ReservationStarting or NotificationType.ArrivalRequired
                or NotificationType.ReservationExpired or NotificationType.ReservationCancelled => "pages/reservations/reservations",
            NotificationType.WaitlistAvailable => "pages/reservations/reservations",
            NotificationType.CreditChanged => "pages/credit/credit",
            NotificationType.ReportResult => "pages/report/report",
            _ => "pages/index/index"
        };

        var msg = BuildTemplateData(type, title, content, data);

        await _wechat.SendSubscribeMessageAsync(user.OpenId, templateId, page, msg, ct);
    }

    // 按通知类型组装订阅消息字段。
    // 注意：字段名（如 thing46/phrase14/date3）必须与微信公众平台所选模板的关键词 ID 完全一致。
    // 「预约成功 / 候补自动预约 / 分享被预约」复用模板「预约通知」（编号 461）：
    // 座位={{thing46}}、预约状态={{phrase14}}、预约时间={{date3}}，
    // 具体值由业务侧通过 data(JSON) 传入 { seat, status, time }，未传时使用默认值。
    private static Dictionary<string, SubscribeDataItem> BuildTemplateData(NotificationType type, string title, string? content, string? data)
    {
        if (type == NotificationType.WaitlistAvailable || type == NotificationType.ReservationCreated)
        {
            var seat = string.Empty;
            var status = "预约成功";
            var timeCn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ChinaTz).ToString("yyyy-MM-dd HH:mm");

            if (!string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("seat", out var s) && s.ValueKind == JsonValueKind.String)
                        seat = s.GetString() ?? seat;
                    if (root.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
                        status = st.GetString() ?? status;
                    if (root.TryGetProperty("time", out var t) && t.ValueKind == JsonValueKind.String
                        && DateTime.TryParse(t.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
                        timeCn = TimeZoneInfo.ConvertTimeFromUtc(dt, ChinaTz).ToString("yyyy-MM-dd HH:mm");
                }
                catch (JsonException) { }
            }

            return new Dictionary<string, SubscribeDataItem>
            {
                ["thing46"] = new SubscribeDataItem(Clip(seat, 20)),
                ["phrase14"] = new SubscribeDataItem(Clip(status, 5)),
                ["date3"] = new SubscribeDataItem(timeCn)
            };
        }

        return new Dictionary<string, SubscribeDataItem>
        {
            ["thing1"] = new SubscribeDataItem(Clip(title, 20)),
            ["thing2"] = new SubscribeDataItem(Clip(content ?? string.Empty, 20))
        };
    }

    private static readonly TimeZoneInfo ChinaTz = TimeZoneInfo.CreateCustomTimeZone(
        "China Standard Time", TimeSpan.FromHours(8), "China Standard Time", "China Standard Time");

    private static string Clip(string s, int max) => s.Length <= max ? s : s[..max];
}
