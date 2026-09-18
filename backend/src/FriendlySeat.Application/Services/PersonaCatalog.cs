namespace FriendlySeat.Application.Services;

/// <summary>
/// 友邻画像题库与类型库（固定问卷 + 规则算分；仅偏好侧写，非心理测评）。
/// 维度各 1–4 分：social 独行↔结伴、rhythm 晨型↔夜型、style 沉浸↔交流、plan 随性↔计划、interest 人文↔实用。
/// </summary>
public static class PersonaCatalog
{
    public const int QuestionsPerDimension = 3;

    public sealed record Option(string Label, int Score);

    public sealed record Question(string Id, string Dimension, string Text, Option[] Options);

    public sealed record PersonaType(string Code, string Name, string Desc, string Advice, string[] MatchCodes);

    public static readonly IReadOnlyList<Question> Questions = new Question[]
    {
        // ===== social 社交能量（独行 ↔ 结伴）=====
        new("s1", "social", "到了自习室，你更希望", new[]
        {
            new Option("一个人找个安静的角落", 1),
            new Option("附近有人，但各忙各的", 2),
            new Option("能和认识的人坐一排", 3),
            new Option("约上一群人才有劲", 4)
        }),
        new("s2", "social", "中场休息的十分钟，你会", new[]
        {
            new Option("继续读下去", 1),
            new Option("戴上耳机看看窗外", 2),
            new Option("和旁边的人聊两句", 3),
            new Option("到处串门找人说说话", 4)
        }),
        new("s3", "social", "新到一个学习群，你通常", new[]
        {
            new Option("先潜水观察", 1),
            new Option("偶尔冒个泡", 2),
            new Option("会回应别人的提问", 3),
            new Option("主动自我介绍、张罗话题", 4)
        }),

        // ===== rhythm 作息节律（晨型 ↔ 夜型）=====
        new("r1", "rhythm", "你效率最高的时段是", new[]
        {
            new Option("清晨", 1),
            new Option("上午", 2),
            new Option("下午到傍晚", 3),
            new Option("深夜", 4)
        }),
        new("r2", "rhythm", "周末的你一般", new[]
        {
            new Option("七八点自然醒", 1),
            new Option("九十点起", 2),
            new Option("中午前后起", 3),
            new Option("下午起、凌晨才睡", 4)
        }),
        new("r3", "rhythm", "图书馆闭馆之后，你会", new[]
        {
            new Option("直接回去休息", 1),
            new Option("回来再看会儿书", 2),
            new Option("刷一会儿手机才睡", 3),
            new Option("换个地方继续学到后半夜", 4)
        }),

        // ===== style 学习方式（沉浸 ↔ 交流）=====
        new("t1", "style", "遇到不懂的概念，你倾向", new[]
        {
            new Option("自己查资料啃透", 1),
            new Option("先记下来慢慢想", 2),
            new Option("找人讨论一下", 3),
            new Option("直接问人，说出来才懂", 4)
        }),
        new("t2", "style", "复习一门课，你更喜欢", new[]
        {
            new Option("安静地自己整理笔记", 1),
            new Option("看录播慢慢过一遍", 2),
            new Option("和白板互相讲", 3),
            new Option("组队刷题、互相提问", 4)
        }),
        new("t3", "style", "小组作业里，你常是", new[]
        {
            new Option("独自把自己那部分做完", 1),
            new Option("按分工完成自己那块", 2),
            new Option("负责和大家对齐进度", 3),
            new Option("组织讨论、主持推进", 4)
        }),

        // ===== plan 计划性（随性 ↔ 计划）=====
        new("p1", "plan", "你的学习安排更像", new[]
        {
            new Option("走到哪学到哪", 1),
            new Option("大概有个方向", 2),
            new Option("每周会列个清单", 3),
            new Option("精确到每天每小时", 4)
        }),
        new("p2", "plan", "面对三个月后的考试，你会", new[]
        {
            new Option("先学起来再说", 1),
            new Option("大致分个阶段", 2),
            new Option("做一份详细计划表", 3),
            new Option("计划表加每周复盘", 4)
        }),
        new("p3", "plan", "你的书桌和笔记", new[]
        {
            new Option("随手能找到就行", 1),
            new Option("大致分个类", 2),
            new Option("分类清楚、有索引", 3),
            new Option("有固定体系，常整理", 4)
        }),

        // ===== interest 兴趣取向（人文 ↔ 实用）=====
        new("i1", "interest", "空闲时你更想读", new[]
        {
            new Option("文学、历史、社科", 1),
            new Option("人物传记、纪实", 2),
            new Option("科普、思维方法", 3),
            new Option("技能、工具、实操", 4)
        }),
        new("i2", "interest", "更打动你的分享是", new[]
        {
            new Option("一段能共情的文字", 1),
            new Option("一个有意思的故事", 2),
            new Option("一个方法或框架", 3),
            new Option("一套能立刻上手的步骤", 4)
        }),
        new("i3", "interest", "你学新东西的动力是", new[]
        {
            new Option("纯粹喜欢", 1),
            new Option("想弄明白为什么", 2),
            new Option("想解决某个问题", 3),
            new Option("想马上用上、做出东西", 4)
        })
    };

    public static readonly IReadOnlyDictionary<string, PersonaType> Types = new Dictionary<string, PersonaType>
    {
        ["morning_solo_focus"] = new("morning_solo_focus", "清晨书虫", "天亮就开始，安静地把计划一点点啃完。", "适合和同样早起、能各自安静的人结伴。", new[] { "morning_group_focus", "night_solo_focus" }),
        ["morning_solo_talk"] = new("morning_solo_talk", "早课笔记党", "起得早、想得多，喜欢先弄懂再开口。", "适合和爱问问题的人凑一对。", new[] { "night_solo_talk", "morning_group_focus" }),
        ["morning_group_focus"] = new("morning_group_focus", "晨读同行者", "喜欢有人一起，但各看各的书。", "适合找同样规律的搭子长期共学。", new[] { "morning_solo_focus", "night_group_focus" }),
        ["morning_group_talk"] = new("morning_group_talk", "晨光召集人", "早起、能聊，常常是那个把人聚起来的人。", "适合和愿意回应的人一起张罗活动。", new[] { "night_solo_talk", "night_group_talk" }),
        ["night_solo_focus"] = new("night_solo_focus", "深夜码农", "越晚越清醒，一个人也能沉浸很久。", "适合和同样不打扰别人的人做远程搭子。", new[] { "morning_solo_focus", "night_group_focus" }),
        ["night_solo_talk"] = new("night_solo_talk", "夜猫提问官", "夜里脑子转得快，问题一个接一个。", "适合和爱讨论的人互相追问。", new[] { "morning_solo_talk", "night_group_talk" }),
        ["night_group_focus"] = new("night_group_focus", "夜读搭子", "喜欢有人陪着，安静地一起把夜坐满。", "适合找作息相近、不爱闲聊的搭子。", new[] { "morning_group_focus", "night_solo_focus" }),
        ["night_group_talk"] = new("night_group_talk", "深夜辩论咖", "夜里话最多，喜欢把一个观点聊透。", "适合和愿意接话的人组读书会。", new[] { "morning_group_talk", "night_solo_talk" })
    };

    /// <summary>按 作息/社交/学习方式 三维定位类型（阈值 2.5）</summary>
    public static string ResolveTypeCode(int social, int rhythm, int style)
    {
        var morning = rhythm < 3;
        var solo = social < 3;
        var focus = style < 3;
        var prefix = morning ? "morning" : "night";
        var mid = solo ? "solo" : "group";
        var tail = focus ? "focus" : "talk";
        var code = $"{prefix}_{mid}_{tail}";
        return Types.ContainsKey(code) ? code : "morning_solo_focus";
    }

    /// <summary>补充标签（用于画像卡展示与匹配解释）</summary>
    public static string[] BuildTags(int social, int rhythm, int style, int plan, int interest)
    {
        return new[]
        {
            rhythm < 3 ? "早起党" : "夜猫子",
            social < 3 ? "独行侠" : "结伴型",
            style < 3 ? "沉浸派" : "爱讨论",
            plan < 3 ? "随性派" : "计划型",
            interest < 3 ? "人文派" : "实用派"
        };
    }

    public static string DimensionLabel(string dimension) => dimension switch
    {
        "social" => "社交能量",
        "rhythm" => "作息节律",
        "style" => "学习方式",
        "plan" => "计划性",
        "interest" => "兴趣取向",
        _ => dimension
    };

    public static string DimensionLow(string dimension) => dimension switch
    {
        "social" => "独行",
        "rhythm" => "晨型",
        "style" => "沉浸",
        "plan" => "随性",
        "interest" => "人文",
        _ => ""
    };

    public static string DimensionHigh(string dimension) => dimension switch
    {
        "social" => "结伴",
        "rhythm" => "夜型",
        "style" => "交流",
        "plan" => "计划",
        "interest" => "实用",
        _ => ""
    };
}
