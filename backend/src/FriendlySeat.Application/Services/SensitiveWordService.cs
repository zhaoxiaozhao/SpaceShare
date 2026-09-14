using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;

namespace FriendlySeat.Application.Services;

/// <summary>
/// 敏感词兜底过滤：词表来自 SystemConfigs（Category=SensitiveWords, ConfigKey=words，逗号/换行分隔）；
/// 未配置时使用内置默认词表。用于用户发布的文字内容（如活动标题/介绍）。
/// </summary>
public class SensitiveWordService
{
    public const string ConfigKey = "words";

    private static readonly string[] DefaultWords =
    {
        // 交易/转让（对应"不卖座、不炒座、不转让"）
        "转让", "出售", "售卖", "买卖", "交易", "出租", "收费", "有偿", "中介", "黄牛", "炒座", "占座", "代占",
        // 导流/联系方式
        "加微信", "加qq", "微信号", "qq号", "私聊", "联系方式", "手机号", "身份证", "扫码进群", "公众号",
        // 广告/欺诈
        "办证", "代开", "发票", "刷单", "兼职", "贷款", "套现", "博彩", "赌博", "红包", "转账", "押金",
        // 违禁
        "色情", "招嫖", "裸聊", "毒品", "枪支", "弹药", "暴恐", "恐怖", "邪教"
    };

    private readonly ConfigService _config;

    public SensitiveWordService(ConfigService config)
    {
        _config = config;
    }

    private async Task<List<string>> GetWordsAsync(CancellationToken ct)
    {
        var raw = await _config.GetValueAsync(ConfigCategory.SensitiveWords, ConfigKey, ct);
        if (string.IsNullOrWhiteSpace(raw)) return DefaultWords.ToList();

        return raw
            .Split(new[] { ',', '，', '\n', '\r', ';', '；', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Distinct()
            .ToList();
    }

    /// <summary>返回命中的第一个敏感词；无则返回 null。</summary>
    public async Task<string?> FirstHitAsync(IEnumerable<string?> texts, CancellationToken ct = default)
    {
        var words = await GetWordsAsync(ct);
        foreach (var t in texts)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;
            var text = t.ToLowerInvariant();
            foreach (var w in words)
            {
                if (text.Contains(w.ToLowerInvariant())) return w;
            }
        }
        return null;
    }
}
