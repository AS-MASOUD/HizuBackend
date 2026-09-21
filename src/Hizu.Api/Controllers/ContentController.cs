using System.Text.Json;
using Hizu.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Api.Controllers;

public class ContentController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;

    public ContentController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("process-steps")]
    public async Task<ActionResult<List<ProcessStepDto>>> GetProcessSteps()
    {
        var steps = await _context.ProcessSteps
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new ProcessStepDto(
                x.Id,
                x.StepNumber,
                x.Title,
                x.Description,
                x.DisplayOrder))
            .ToListAsync();

        return Ok(steps);
    }

    [HttpGet("services")]
    public async Task<ActionResult<List<FabricationServiceDto>>> GetServices()
    {
        var services = await _context.FabricationServices
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        var dtos = services.Select(MapServiceToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("services/{slug}")]
    public async Task<ActionResult<FabricationServiceDto>> GetServiceBySlug(string slug)
    {
        var service = await _context.FabricationServices
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);

        if (service == null)
            return NotFound("سرویس مورد نظر یافت نشد.");

        return Ok(MapServiceToDto(service));
    }

    [HttpGet("faqs")]
    public async Task<ActionResult<List<FaqItemDto>>> GetFaqs()
    {
        var faqs = await _context.FaqItems
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new FaqItemDto(
                x.Id,
                x.Question,
                x.Answer,
                x.Badge,
                x.DisplayOrder))
            .ToListAsync();

        return Ok(faqs);
    }

    [HttpGet("slides")]
    public async Task<ActionResult<List<HeroSlideDto>>> GetSlides()
    {
        var slides = await _context.HeroSlides
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new HeroSlideDto(
                x.Id,
                x.Title,
                x.Subtitle,
                x.Badge,
                x.ImageUrl,
                x.DisplayOrder))
            .ToListAsync();

        return Ok(slides);
    }

    [HttpGet("about")]
    public async Task<ActionResult<AboutContentDto>> GetAboutContent()
    {
        var team = await _context.TeamMembers
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        var goals = await _context.CompanyGoals
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CompanyGoalDto(
                x.Id,
                x.GoalNumber,
                x.Title,
                x.Subtitle,
                x.Description,
                x.IconName,
                x.DisplayOrder))
            .ToListAsync();

        var milestones = await _context.CompanyMilestones
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CompanyMilestoneDto(
                x.Id,
                x.Year,
                x.Title,
                x.Description,
                x.DisplayOrder))
            .ToListAsync();

        var teamDtos = team.Select(t => new TeamMemberDto(
            t.Id,
            t.Name,
            t.Role,
            t.EnglishRole,
            t.Education,
            t.ImageUrl,
            t.Bio,
            SafeDeserializeList<string>(t.SpecialtiesJson),
            t.Email,
            t.DisplayOrder)).ToList();

        return Ok(new AboutContentDto(teamDtos, goals, milestones));
    }

    [HttpGet("home")]
    public async Task<ActionResult<HomeContentDto>> GetHomeContent()
    {
        var steps = await _context.ProcessSteps
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new ProcessStepDto(x.Id, x.StepNumber, x.Title, x.Description, x.DisplayOrder))
            .ToListAsync();

        var servicesList = await _context.FabricationServices
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        var services = servicesList.Select(MapServiceToDto).ToList();

        var faqs = await _context.FaqItems
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new FaqItemDto(x.Id, x.Question, x.Answer, x.Badge, x.DisplayOrder))
            .ToListAsync();

        var slides = await _context.HeroSlides
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new HeroSlideDto(x.Id, x.Title, x.Subtitle, x.Badge, x.ImageUrl, x.DisplayOrder))
            .ToListAsync();

        return Ok(new HomeContentDto(steps, services, faqs, slides));
    }

    private static FabricationServiceDto MapServiceToDto(Hizu.Domain.Entities.FabricationService s)
    {
        return new FabricationServiceDto(
            s.Id,
            s.Slug,
            s.Code,
            s.Badge,
            s.Coord,
            s.Title,
            s.ShortTitle,
            s.Subtitle,
            s.Description,
            s.ShortDescription,
            s.ImageUrl,
            s.IconName,
            SafeDeserializeList<ServiceSpecDto>(s.SpecsJson),
            SafeDeserializeList<string>(s.MaterialsJson),
            SafeDeserializeList<string>(s.ApplicationsJson),
            s.Equipment,
            s.DisplayOrder);
    }

    private static List<T> SafeDeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public record ProcessStepDto(
    Guid Id,
    string StepNumber,
    string Title,
    string Description,
    int DisplayOrder);

public record ServiceSpecDto(
    string Label,
    string Value);

public record FabricationServiceDto(
    Guid Id,
    string Slug,
    string Code,
    string Badge,
    string Coord,
    string Title,
    string ShortTitle,
    string Subtitle,
    string Description,
    string ShortDescription,
    string ImageUrl,
    string IconName,
    List<ServiceSpecDto> Specs,
    List<string> Materials,
    List<string> Applications,
    string Equipment,
    int DisplayOrder);

public record FaqItemDto(
    Guid Id,
    string Question,
    string Answer,
    string Badge,
    int DisplayOrder);

public record HeroSlideDto(
    Guid Id,
    string Title,
    string Subtitle,
    string Badge,
    string ImageUrl,
    int DisplayOrder);

public record TeamMemberDto(
    Guid Id,
    string Name,
    string Role,
    string EnglishRole,
    string Education,
    string ImageUrl,
    string Bio,
    List<string> Specialties,
    string Email,
    int DisplayOrder);

public record CompanyGoalDto(
    Guid Id,
    string GoalNumber,
    string Title,
    string Subtitle,
    string Description,
    string IconName,
    int DisplayOrder);

public record CompanyMilestoneDto(
    Guid Id,
    string Year,
    string Title,
    string Description,
    int DisplayOrder);

public record AboutContentDto(
    List<TeamMemberDto> TeamMembers,
    List<CompanyGoalDto> Goals,
    List<CompanyMilestoneDto> Milestones);

public record HomeContentDto(
    List<ProcessStepDto> ProcessSteps,
    List<FabricationServiceDto> Services,
    List<FaqItemDto> Faqs,
    List<HeroSlideDto> Slides);
