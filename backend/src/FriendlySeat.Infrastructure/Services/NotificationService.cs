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
        [NotificationType.System] = "system",
        [NotificationType.ActivityReview] = "activity_review"
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
            NotificationType.ActivityReview => "pages/activity/activity",
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

        // 到座提醒复用模板「签到提醒」（编号 513）：座位=short_thing32、入座倒计时=short_thing33、截止时间=time5
        if (type == NotificationType.ArrivalRequired)
        {
            var seat = string.Empty;
            var countdown = string.Empty;
            var deadlineCn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ChinaTz).ToString("yyyy-MM-dd HH:mm");

            if (!string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("seat", out var s) && s.ValueKind == JsonValueKind.String)
                        seat = s.GetString() ?? seat;
                    if (root.TryGetProperty("countdown", out var c) && c.ValueKind == JsonValueKind.String)
                        countdown = c.GetString() ?? countdown;
                    if (root.TryGetProperty("deadline", out var d) && d.ValueKind == JsonValueKind.String
                        && DateTime.TryParse(d.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dd))
                        deadlineCn = TimeZoneInfo.ConvertTimeFromUtc(dd, ChinaTz).ToString("yyyy-MM-dd HH:mm");
                }
                catch (JsonException) { }
            }

            return new Dictionary<string, SubscribeDataItem>
            {
                ["short_thing32"] = new SubscribeDataItem(Clip(seat, 20)),
                ["short_thing33"] = new SubscribeDataItem(Clip(countdown, 10)),
                ["time5"] = new SubscribeDataItem(deadlineCn)
            };
        }

        // 预约过期/爽约、分享被取消 复用模板「预约通知」：预约状态=phrase14、座位=thing46、备注=thing7
        if (type == NotificationType.ReservationExpired || type == NotificationType.ReservationCancelled)
        {
            var isExpired = type == NotificationType.ReservationExpired;
            var seat = string.Empty;
            var status = isExpired ? "爽约" : "已取消";
            var remark = isExpired ? "请按时到座，以免影响信用" : "分享被取消，座位已释放";

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
                    if (root.TryGetProperty("remark", out var rk) && rk.ValueKind == JsonValueKind.String)
                        remark = rk.GetString() ?? remark;
                }
                catch (JsonException) { }
            }

            return new Dictionary<string, SubscribeDataItem>
            {
                ["phrase14"] = new SubscribeDataItem(Clip(status, 5)),
                ["thing46"] = new SubscribeDataItem(Clip(seat, 20)),
                ["thing7"] = new SubscribeDataItem(Clip(remark, 20))
            };
        }

        // 活动审核结果复用模板「审核通过通知」（编号 895）：审核结果=phrase1、审核内容=thing2、审核时间=date3
        if (type == NotificationType.ActivityReview)
        {
            var result = "通过";
            var contentVal = string.Empty;
            var timeCn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ChinaTz).ToString("yyyy-MM-dd HH:mm:ss");

            if (!string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("result", out var rs) && rs.ValueKind == JsonValueKind.String)
                        result = rs.GetString() ?? result;
                    if (root.TryGetProperty("content", out var ctt) && ctt.ValueKind == JsonValueKind.String)
                        contentVal = ctt.GetString() ?? contentVal;
                    if (root.TryGetProperty("time", out var tm) && tm.ValueKind == JsonValueKind.String
                        && DateTime.TryParse(tm.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var tdt))
                        timeCn = TimeZoneInfo.ConvertTimeFromUtc(tdt, ChinaTz).ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (JsonException) { }
            }

            return new Dictionary<string, SubscribeDataItem>
            {
                ["phrase1"] = new SubscribeDataItem(Clip(result, 5)),
                ["thing2"] = new SubscribeDataItem(Clip(contentVal, 20)),
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
