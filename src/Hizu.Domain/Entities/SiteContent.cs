using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class ProcessStep : BaseEntity
{
    public string StepNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FabricationService : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string Coord { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ShortTitle { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public string SpecsJson { get; set; } = "[]";
    public string MaterialsJson { get; set; } = "[]";
    public string ApplicationsJson { get; set; } = "[]";
    public string Equipment { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FaqItem : BaseEntity
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class HeroSlide : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TeamMember : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string EnglishRole { get; set; } = string.Empty;
    public string Education { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string SpecialtiesJson { get; set; } = "[]";
    public string Email { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CompanyGoal : BaseEntity
{
    public string GoalNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CompanyMilestone : BaseEntity
{
    public string Year { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
