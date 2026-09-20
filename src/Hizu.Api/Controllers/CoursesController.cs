using Hizu.Application.Common.Models;
using Hizu.Application.Features.Courses.DTOs;
using Hizu.Application.Features.Courses.Queries;
using Hizu.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

public class CoursesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CourseListDto>>> GetCourses(
        [FromQuery] string? search,
        [FromQuery] CourseLevel? level,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var result = await Mediator.Send(new GetCoursesQuery(search, level, page, pageSize));
        return Ok(result);
    }

    [HttpGet("featured")]
    public async Task<ActionResult<List<CourseListDto>>> GetFeaturedCourses([FromQuery] int count = 3)
    {
        var result = await Mediator.Send(new GetFeaturedCoursesQuery(count));
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<CourseDto>> GetCourseBySlug(string slug)
    {
        var result = await Mediator.Send(new GetCourseBySlugQuery(slug));
        return Ok(result);
    }
}
