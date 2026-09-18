namespace FriendlySeat.Application.Dtos;

public class PersonaOptionDto
{
    public string Label { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class PersonaQuestionDto
{
    public string Id { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public string DimensionLabel { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<PersonaOptionDto> Options { get; set; } = new();
}

public class PersonaAnswerDto
{
    public string QuestionId { get; set; } = string.Empty;
    public int OptionIndex { get; set; }
}

public class PersonaSubmitRequest
{
    public List<PersonaAnswerDto> Answers { get; set; } = new();
}

public class PersonaDimensionDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Low { get; set; } = string.Empty;
    public string High { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class PersonaMatchDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
}

public class PersonaPairGroupDto
{
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<PersonaMatchDto> Items { get; set; } = new();
}

public class PersonaProfileDto
{
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string TypeDesc { get; set; } = string.Empty;
    public string Quote { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Scene { get; set; } = string.Empty;
    public string Poet { get; set; } = string.Empty;
    public string PoetLine { get; set; } = string.Empty;
    public string PoetWhy { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public List<PersonaDimensionDto> Dimensions { get; set; } = new();
    public List<PersonaPairGroupDto> Pairings { get; set; } = new();
    public bool IsPublic { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PersonaVisibilityRequest
{
    public bool IsPublic { get; set; }
}
