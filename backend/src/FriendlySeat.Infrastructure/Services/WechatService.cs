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

    public WechatService(HttpClient http, IOptions<WechatOptions> options, ILogger<WechatService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
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

    private class WechatSessionResponse
    {
        [JsonPropertyName("openid")] public string? OpenId { get; set; }
        [JsonPropertyName("unionid")] public string? UnionId { get; set; }
        [JsonPropertyName("session_key")] public string? SessionKey { get; set; }
        [JsonPropertyName("errcode")] public int ErrCode { get; set; }
        [JsonPropertyName("errmsg")] public string? ErrMsg { get; set; }
    }
}
