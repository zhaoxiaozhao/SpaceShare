using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>友邻画像：固定问卷 + 规则算分，生成偏好侧写（非心理测评）。默认私密。</summary>
public class PersonaService
{
    private static readonly string[] Dimensions = { "social", "rhythm", "style", "plan", "interest", "focus", "motive" };

    private readonly IAppDbContext _db;
    private readonly IWechatService _wechat;

    public PersonaService(IAppDbContext db, IWechatService wechat)
    {
        _db = db;
        _wechat = wechat;
    }

    public Task<List<PersonaQuestionDto>> GetQuestionsAsync(CancellationToken ct = default)
    {
        var list = PersonaCatalog.Questions.Select(q => new PersonaQuestionDto
        {
            Id = q.Id,
            Dimension = q.Dimension,
            DimensionLabel = PersonaCatalog.DimensionLabel(q.Dimension),
            Text = q.Text,
            Options = q.Options.Select(o => new PersonaOptionDto { Label = o.Label, Score = o.Score }).ToList()
        }).ToList();

        return Task.FromResult(list);
    }

    public async Task<PersonaProfileDto> SubmitAsync(long userId, PersonaSubmitRequest request, CancellationToken ct = default)
    {
        var answers = (request.Answers ?? new List<PersonaAnswerDto>())
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().OptionIndex);

        var sums = Dimensions.ToDictionary(d => d, _ => 0);
        var counts = Dimensions.ToDictionary(d => d, _ => 0);

        foreach (var q in PersonaCatalog.Questions)
        {
            if (!answers.TryGetValue(q.Id, out var idx)) continue;
            if (idx < 0 || idx >= q.Options.Length) continue;
            sums[q.Dimension] += q.Options[idx].Score;
            counts[q.Dimension]++;
        }

        if (Dimensions.Any(d => counts[d] == 0))
            throw AppException.BadRequest("answers_incomplete", "请先完成全部题目");

        int Score(string d) => Math.Clamp((int)Math.Round((double)sums[d] / counts[d]), 1, 4);

        var social = Score("social");
        var rhythm = Score("rhythm");
        var style = Score("style");
        var plan = Score("plan");
        var interest = Score("interest");
        var focus = Score("focus");
        var motive = Score("motive");

        var typeCode = PersonaCatalog.ResolveTypeCode(social, rhythm, style, plan);

        var profile = await _db.PersonaProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
        {
            profile = new PersonaProfile { UserId = userId, CreatedAt = DateTime.UtcNow };
            _db.PersonaProfiles.Add(profile);
        }
        profile.Social = social;
        profile.Rhythm = rhythm;
        profile.Style = style;
        profile.Plan = plan;
        profile.Interest = interest;
        profile.Focus = focus;
        profile.Motive = motive;
        profile.TypeCode = typeCode;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(profile);
    }

    public async Task<PersonaProfileDto?> GetMineAsync(long userId, CancellationToken ct = default)
    {
        var profile = await _db.PersonaProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return profile is null ? null : ToDto(profile);
    }

    public async Task<PersonaProfileDto> SetVisibilityAsync(long userId, bool isPublic, CancellationToken ct = default)
    {
        var profile = await _db.PersonaProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw AppException.NotFound("还没有生成画像");
        profile.IsPublic = isPublic;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(profile);
    }

    /// <summary>画像海报用：昵称 + 头像（云存储 fileID 由后端代理取回并转 base64）</summary>
    public async Task<PersonaAvatarDto> GetAvatarAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.Users.Where(u => u.Id == userId)
            .Select(u => new { u.Nickname, u.AvatarUrl })
            .FirstOrDefaultAsync(ct);
        if (user is null) return new PersonaAvatarDto();

        string? dataUrl = null;
        var url = user.AvatarUrl;
        if (!string.IsNullOrWhiteSpace(url))
        {
            if (url.StartsWith("cloud://", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await _wechat.DownloadCloudFileAsync(url, ct);
                if (bytes is { Length: > 0 } && bytes.Length <= 2_000_000)
                {
                    dataUrl = $"data:{DetectImageMime(bytes)};base64,{Convert.ToBase64String(bytes)}";
                }
            }
            else
            {
                dataUrl = url;
            }
        }

        return new PersonaAvatarDto { Name = user.Nickname ?? "友邻", DataUrl = dataUrl };
    }

    private static string DetectImageMime(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8) return "image/jpeg";
        if (b.Length >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
        if (b.Length >= 12 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
        if (b.Length >= 3 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return "image/gif";
        return "image/jpeg";
    }

    private static PersonaProfileDto ToDto(PersonaProfile p)
    {
        var type = PersonaCatalog.Types.TryGetValue(p.TypeCode, out var t)
            ? t
            : PersonaCatalog.Types["morning_solo_immerser"];

        var dims = new Dictionary<string, int>
        {
            ["social"] = p.Social,
            ["rhythm"] = p.Rhythm,
            ["style"] = p.Style,
            ["plan"] = p.Plan,
            ["interest"] = p.Interest,
            ["focus"] = p.Focus,
            ["motive"] = p.Motive
        };

        var (high, complement, warmup) = PersonaCatalog.BuildPairings(type.Code);

        static List<PersonaMatchDto> Map(IEnumerable<PersonaCatalog.PersonaType> list) => list
            .Select(x => new PersonaMatchDto { Code = x.Code, Name = x.Name, Desc = x.Desc })
            .ToList();

        return new PersonaProfileDto
        {
            TypeCode = type.Code,
            TypeName = type.Name,
            TypeDesc = type.Desc,
            Quote = type.Quote,
            Color = type.Color,
            Scene = type.Scene,
            Poet = type.Poet,
            PoetLine = type.PoetLine,
            PoetWhy = type.PoetWhy,
            Tags = PersonaCatalog.BuildTags(p.Social, p.Rhythm, p.Style, p.Plan, p.Interest, p.Focus, p.Motive).ToList(),
            Roles = PersonaCatalog.BuildRoles(p.Social, p.Style, p.Plan, p.Focus, p.Motive).ToList(),
            Dimensions = dims.Select(kv => new PersonaDimensionDto
            {
                Key = kv.Key,
                Label = PersonaCatalog.DimensionLabel(kv.Key),
                Low = PersonaCatalog.DimensionLow(kv.Key),
                High = PersonaCatalog.DimensionHigh(kv.Key),
                Score = kv.Value
            }).ToList(),
            Pairings = new List<PersonaPairGroupDto>
            {
                new() { Title = "高契合搭子", Reason = "作息、节奏都合得上，约起来最省心", Items = Map(high) },
                new() { Title = "互补搭子", Reason = "有些地方不一样，正好互相补位", Items = Map(complement) },
                new() { Title = "需要磨合", Reason = "作息或节奏差得较多，建议先商量好规则", Items = Map(warmup) }
            },
            IsPublic = p.IsPublic,
            UpdatedAt = p.UpdatedAt
        };
    }
}
