namespace FriendlySeat.Application.Services;

/// <summary>
/// 友邻画像题库与类型库（固定问卷 + 规则算分；仅偏好侧写，非心理测评）。
/// 维度各 1–4 分：social 社交能量、rhythm 作息节律、style 学习方式、plan 计划性、interest 兴趣取向、focus 专注节奏、motive 共学动机。
/// </summary>
public static class PersonaCatalog
{
    public sealed record Option(string Label, int Score);

    public sealed record Question(string Id, string Dimension, string Text, Option[] Options);

    public sealed record PersonaType(string Code, string Name, string Desc, string Quote, string Color, string Scene, string Poet, string PoetLine, string PoetWhy);

    public sealed record PairLevel(string Title, string Reason);

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
        }),

        // ===== focus 专注节奏（短冲刺 ↔ 长时段沉浸）=====
        new("f1", "focus", "你更舒服的学习节奏是", new[]
        {
            new Option("25 分钟一小段，频繁休息", 1),
            new Option("45 分钟左右一段", 2),
            new Option("一个多小时连贯", 3),
            new Option("一坐就是两三小时", 4)
        }),
        new("f2", "focus", "被打断之后，你", new[]
        {
            new Option("很快能切回来", 1),
            new Option("需要几分钟缓一下", 2),
            new Option("要重新找回状态", 3),
            new Option("很难再回到刚才的状态", 4)
        }),
        new("f3", "focus", "理想的单次自习时长是", new[]
        {
            new Option("一小时以内", 1),
            new Option("一个半小时", 2),
            new Option("两到三小时", 3),
            new Option("三小时以上", 4)
        }),

        // ===== motive 共学动机（自我驱动 ↔ 陪伴监督）=====
        new("m1", "motive", "你更需要搭子提供", new[]
        {
            new Option("各学各的就好", 1),
            new Option("偶尔对一下进度", 2),
            new Option("有人一起更能坚持", 3),
            new Option("有人催我、监督我", 4)
        }),
        new("m2", "motive", "一个人学不下去时，你会", new[]
        {
            new Option("自己想办法调整", 1),
            new Option("换个环境继续", 2),
            new Option("约个朋友一起", 3),
            new Option("必须有个人盯着", 4)
        }),
        new("m3", "motive", "你报名共学活动的原因", new[]
        {
            new Option("内容本身吸引我", 1),
            new Option("想认识同好", 2),
            new Option("需要一点氛围", 3),
            new Option("需要外部约束", 4)
        })
    };

    public static readonly IReadOnlyDictionary<string, PersonaType> Types = new Dictionary<string, PersonaType>
    {
        // ===== 晨 · 独行 =====
        ["morning_solo_immerser"] = new("morning_solo_immerser", "清晨书虫", "天亮就开始，安静地把计划一点点啃完。", "先把今天最难的那一页翻过去。", "#6BAF8B", "适合清晨自习、考研早起档", "陶渊明", "采菊东篱下，悠然见南山", "你喜欢一个人，在清晨把日子过得有节奏。"),
        ["morning_solo_talker"] = new("morning_solo_talker", "早课提问党", "起得早、想得多，喜欢先弄懂再开口。", "想清楚再问，问出来就懂了。", "#2E8B94", "适合共读答疑、线上问题接龙", "韩愈", "业精于勤，荒于嬉", "你起得早，更要紧的是把问题想清楚。"),
        ["morning_solo_host"] = new("morning_solo_host", "晨间规划师", "早起、有计划，喜欢把一天先排明白。", "计划写下来的那一刻，就已经开始做了。", "#C98A3D", "适合长期备考、每周复盘小组", "范仲淹", "先天下之忧而忧", "你习惯先规划，再动手。"),
        // ===== 晨 · 结伴 =====
        ["morning_group_immerser"] = new("morning_group_immerser", "晨读同行者", "喜欢有人一起，但各看各的书。", "安静地并肩，比热闹更长久。", "#8CC5A8", "适合图书馆共读、安静自习团", "王维", "行到水穷处，坐看云起时", "你喜欢有人在旁，各看各的书。"),
        ["morning_group_talker"] = new("morning_group_talker", "早茶辩论队", "早上就话多，边吃边把一个话题聊开。", "一个观点聊开心了，一天都顺。", "#57AFB8", "适合读书会、话题早餐局", "苏轼", "一蓑烟雨任平生", "你早上就话多，观点聊开心了一天都顺。"),
        ["morning_group_host"] = new("morning_group_host", "晨光召集人", "早起、能聊，常常是那个把人聚起来的人。", "只要我先开个头，大家就都来了。", "#DBA968", "适合组织晨间共学、活动发起", "欧阳修", "醉翁之意不在酒", "你擅长把大家聚起来。"),
        // ===== 夜 · 独行 =====
        ["night_solo_immerser"] = new("night_solo_immerser", "深夜码农", "越晚越清醒，一个人也能沉浸很久。", "夜一安静下来，效率就上来了。", "#5B6E8C", "适合熬夜冲刺、远程各自自习", "贾岛", "两句三年得，一吟双泪流", "你在深夜和细节较劲。"),
        ["night_solo_talker"] = new("night_solo_talker", "夜猫提问官", "夜里脑子转得快，问题一个接一个。", "问题想不通，就先丢出来。", "#7E90AC", "适合夜间答疑、互问互答", "屈原", "路漫漫其修远兮，吾将上下而求索", "你问题一个接一个，非要问到底。"),
        ["night_solo_host"] = new("night_solo_host", "深夜规划员", "夜里静下来，喜欢把计划和复盘做掉。", "白天做事，晚上收尾。", "#43536B", "适合每周复盘、长期目标管理", "曹操", "老骥伏枥，志在千里", "夜里静下来，你把计划排明白。"),
        // ===== 夜 · 结伴 =====
        ["night_group_immerser"] = new("night_group_immerser", "夜读搭子", "喜欢有人陪着，安静地一起把夜坐满。", "不用说话，知道你在就好。", "#8A93A8", "适合夜间安静共坐、晚自习", "李清照", "赌书消得泼茶香", "你想有人陪着，安静地把夜坐满。"),
        ["night_group_talker"] = new("night_group_talker", "深夜辩论咖", "夜里话最多，喜欢把一个观点聊透。", "把话说透，比憋着舒服。", "#A56C24", "适合读书辩论、友邻夜谈", "李白", "天生我材必有用", "你夜里话最多，喜欢把观点聊透。"),
        ["night_group_host"] = new("night_group_host", "夜谈主持人", "夜里把大家聚起来，什么都聊一点。", "夜里的话，往往最真。", "#6BAF8B", "适合线上夜谈、活动收尾复盘", "柳永", "今宵酒醒何处，杨柳岸晓风残月", "你总能把夜谈接住。"),
    };

    public static readonly string[] RoleKeys = { "规划者", "执行者", "陪读者", "破冰者" };

    /// <summary>按 作息/社交/学习方式 定位 12 类型（阈值 3）</summary>
    public static string ResolveTypeCode(int social, int rhythm, int style, int plan)
    {
        var family = (rhythm < 3 ? "morning" : "night") + "_" + (social < 3 ? "solo" : "group");
        string styleKey;
        if (style < 3) styleKey = "immerser";
        else styleKey = plan < 3 ? "talker" : "host";
        var code = $"{family}_{styleKey}";
        return Types.ContainsKey(code) ? code : "morning_solo_immerser";
    }

    /// <summary>补充标签（用于画像卡展示）</summary>
    public static string[] BuildTags(int social, int rhythm, int style, int plan, int interest, int focus, int motive)
    {
        return new[]
        {
            rhythm < 3 ? "早起党" : "夜猫子",
            social < 3 ? "独行侠" : "结伴型",
            style < 3 ? "沉浸派" : "爱讨论",
            plan < 3 ? "随性派" : "计划型",
            focus < 3 ? "短冲刺" : "长时段",
            motive < 3 ? "自我驱动" : "需要陪伴",
            interest < 3 ? "人文派" : "实用派"
        };
    }

    /// <summary>搭子角色标签（可多个）</summary>
    public static string[] BuildRoles(int social, int style, int plan, int focus, int motive)
    {
        var roles = new List<string>();
        if (plan >= 3) roles.Add("规划者");
        if (plan < 3 && focus < 3) roles.Add("执行者");
        if (motive >= 3) roles.Add("陪读者");
        if (social >= 3 && style >= 3) roles.Add("破冰者");
        if (roles.Count == 0) roles.Add("自带节奏");
        return roles.ToArray();
    }

    // 各类型的代表维度向量（用于两人配型打分）
    private static (double Rhythm, double Social, double Style, double Plan) Vector(string code)
    {
        var parts = code.Split('_');
        var rhythm = parts[0] == "morning" ? 1.5 : 3.5;
        var social = parts[1] == "solo" ? 1.5 : 3.5;
        var (style, plan) = parts[2] switch
        {
            "immerser" => (1.5, 2.0),
            "talker" => (3.5, 2.0),
            _ => (3.5, 3.5)
        };
        return (rhythm, social, style, plan);
    }

    private static int PairScore(string a, string b)
    {
        var x = Vector(a);
        var y = Vector(b);
        var score = 100;
        if (Math.Abs(x.Rhythm - y.Rhythm) > 1.5) score -= 32;
        if (Math.Abs(x.Social - y.Social) > 1.5) score -= 14;
        if (Math.Abs(x.Style - y.Style) > 1.5) score -= 6;   // 学习方式互补反而是加分项，小扣
        if (Math.Abs(x.Plan - y.Plan) > 1.5) score -= 12;
        return score;
    }

    /// <summary>按三档返回契合配型：高契合 / 互补 / 需磨合</summary>
    public static (List<PersonaType> High, List<PersonaType> Complement, List<PersonaType> Warmup) BuildPairings(string myCode)
    {
        var ranked = Types.Values
            .Where(t => t.Code != myCode)
            .Select(t => (Type: t, Score: PairScore(myCode, t.Code)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Type.Code)
            .ToList();

        var high = ranked.Take(3).Select(x => x.Type).ToList();
        var complement = ranked.Skip(3).Take(4).Select(x => x.Type).ToList();
        var warmup = ranked.Skip(7).Select(x => x.Type).ToList();
        return (high, complement, warmup);
    }

    public static string DimensionLabel(string dimension) => dimension switch
    {
        "social" => "社交能量",
        "rhythm" => "作息节律",
        "style" => "学习方式",
        "plan" => "计划性",
        "interest" => "兴趣取向",
        "focus" => "专注节奏",
        "motive" => "共学动机",
        _ => dimension
    };

    public static string DimensionLow(string dimension) => dimension switch
    {
        "social" => "独行",
        "rhythm" => "晨型",
        "style" => "沉浸",
        "plan" => "随性",
        "interest" => "人文",
        "focus" => "短冲刺",
        "motive" => "自我驱动",
        _ => ""
    };

    public static string DimensionHigh(string dimension) => dimension switch
    {
        "social" => "结伴",
        "rhythm" => "夜型",
        "style" => "交流",
        "plan" => "计划",
        "interest" => "实用",
        "focus" => "长时段",
        "motive" => "陪伴监督",
        _ => ""
    };
}
