using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FriendlySeat.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FriendlySeat.Infrastructure.Services;

public class WechatOptions
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}

public class WechatService : IWechatService
{
    private readonly HttpClient _http;
    private readonly WechatOptions _options;
    private readonly ILogger<WechatService> _logger;
    private readonly IRedisCache _cache;

    public WechatService(HttpClient http, IOptions<WechatOptions> options, ILogger<WechatService> logger, IRedisCache cache)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _cache = cache;
    }

    public async Task<WechatSessionResult> Code2SessionAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.AppId) || string.IsNullOrEmpty(_options.AppSecret))
        {
            // 本地开发/测试模式：未配置微信凭据时，使用模拟 openid（稳定前缀，避免与真实 openid 冲突）
            _logger.LogWarning("微信 AppId/AppSecret 未配置，使用模拟登录");
            var openId = code.StartsWith("mock_")
                ? code
                : "mock_" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)))[..32];
            return new WechatSessionResult(openId, null);
        }

        var url = $"https://api.weixin.qq.com/sns/jscode2session?appid={_options.AppId}&secret={_options.AppSecret}&js_code={code}&grant_type=authorization_code";
        var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<WechatSessionResponse>(ct);
        if (result is null || !string.IsNullOrEmpty(result.ErrMsg))
        {
            _logger.LogWarning("微信 code2session 失败: {ErrCode} {ErrMsg}", result?.ErrCode, result?.ErrMsg);
            throw AppException.BadRequest("wechat_code_invalid", "微信登录失败");
        }

        return new WechatSessionResult(result.OpenId!, result.UnionId);
    }

    public async Task<bool> SendSubscribeMessageAsync(string openId, string templateId, string page, IDictionary<string, SubscribeDataItem> data, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(openId) || string.IsNullOrEmpty(templateId)) return false;

        string? accessToken;
        try
        {
            accessToken = await GetAccessTokenAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取微信 access_token 失败，跳过订阅消息发送");
            return false;
        }
        if (string.IsNullOrEmpty(accessToken)) return false;

        try
        {
            var url = $"https://api.weixin.qq.com/cgi-bin/message/subscribe/send?access_token={Uri.EscapeDataString(accessToken)}";
            var payload = new
            {
                touser = openId,
                template_id = templateId,
                page = string.IsNullOrEmpty(page) ? "pages/index/index" : page,
                data = data.ToDictionary(kv => kv.Key, kv => new { value = kv.Value.Value })
            };
            var resp = await _http.PostAsJsonAsync(url, payload, ct);
            var result = await resp.Content.ReadFromJsonAsync<SubscribeMessageResponse>(ct);

            // errcode 0=成功；43101=用户未订阅，不视为错误（静默忽略）
            if (result is null || result.ErrCode != 0)
            {
                if (result?.ErrCode is null or 43101) return false;
                _logger.LogWarning("微信订阅消息发送失败: {ErrCode} {ErrMsg}", result?.ErrCode, result?.ErrMsg);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发送微信订阅消息异常 openId={openId}", openId);
            return false;
        }
    }

    // access_token 有效期 7200s，提前缓存 7000s
    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        const string cacheKey = "wechat:access_token";
        var cached = await _cache.GetAsync<string>(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached)) return cached;

        if (string.IsNullOrEmpty(_options.AppId) || string.IsNullOrEmpty(_options.AppSecret)) return null;

        var url = $"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={_options.AppId}&secret={_options.AppSecret}";
        var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<AccessTokenResponse>(ct);
        if (result is null || string.IsNullOrEmpty(result.AccessToken))
        {
            _logger.LogWarning("获取微信 access_token 失败: {ErrCode} {ErrMsg}", result?.ErrCode, result?.ErrMsg);
            return null;
        }

        await _cache.SetAsync(cacheKey, result.AccessToken, TimeSpan.FromSeconds(7000), ct);
        return result.AccessToken;
    }

    private class WechatSessionResponse
    {
        [JsonPropertyName("openid")] public string? OpenId { get; set; }
        [JsonPropertyName("unionid")] public string? UnionId { get; set; }
        [JsonPropertyName("session_key")] public string? SessionKey { get; set; }
        [JsonPropertyName("errcode")] public int ErrCode { get; set; }
        [JsonPropertyName("errmsg")] public string? ErrMsg { get; set; }
    }

    private class AccessTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("errcode")] public int ErrCode { get; set; }
        [JsonPropertyName("errmsg")] public string? ErrMsg { get; set; }
    }

    private class SubscribeMessageResponse
    {
        [JsonPropertyName("errcode")] public int ErrCode { get; set; }
        [JsonPropertyName("errmsg")] public string? ErrMsg { get; set; }
    }
}
