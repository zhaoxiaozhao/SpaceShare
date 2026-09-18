using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>友邻画像：固定问卷 + 规则算分，生成偏好侧写（非心理测评）。默认私密。</summary>
public class PersonaService
{
    private static readonly string[] Dimensions = { "social", "rhythm", "style", "plan", "interest" };

    private readonly IAppDbContext _db;

    public PersonaService(IAppDbContext db)
    {
        _db = db;
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

        var missing = Dimensions.Where(d => counts[d] == 0).ToList();
        if (missing.Count > 0)
            throw AppException.BadRequest("answers_incomplete", "请先完成全部题目");

        int Score(string d) => Math.Clamp((int)Math.Round((double)sums[d] / counts[d]), 1, 4);

        var social = Score("social");
        var rhythm = Score("rhythm");
        var style = Score("style");
        var plan = Score("plan");
        var interest = Score("interest");

        var typeCode = PersonaCatalog.ResolveTypeCode(social, rhythm, style);

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

    private static PersonaProfileDto ToDto(PersonaProfile p)
    {
        var type = PersonaCatalog.Types.TryGetValue(p.TypeCode, out var t) ? t : PersonaCatalog.Types["morning_solo_focus"];
        var dims = new Dictionary<string, int>
        {
            ["social"] = p.Social,
            ["rhythm"] = p.Rhythm,
            ["style"] = p.Style,
            ["plan"] = p.Plan,
            ["interest"] = p.Interest
        };

        return new PersonaProfileDto
        {
            TypeCode = type.Code,
            TypeName = type.Name,
            TypeDesc = type.Desc,
            Advice = type.Advice,
            Tags = PersonaCatalog.BuildTags(p.Social, p.Rhythm, p.Style, p.Plan, p.Interest).ToList(),
            Dimensions = dims.Select(kv => new PersonaDimensionDto
            {
                Key = kv.Key,
                Label = PersonaCatalog.DimensionLabel(kv.Key),
                Low = PersonaCatalog.DimensionLow(kv.Key),
                High = PersonaCatalog.DimensionHigh(kv.Key),
                Score = kv.Value
            }).ToList(),
            Matches = type.MatchCodes
                .Where(code => PersonaCatalog.Types.ContainsKey(code))
                .Select(code =>
                {
                    var m = PersonaCatalog.Types[code];
                    return new PersonaMatchDto { Code = m.Code, Name = m.Name, Desc = m.Desc };
                })
                .ToList(),
            IsPublic = p.IsPublic,
            UpdatedAt = p.UpdatedAt
        };
    }
}
