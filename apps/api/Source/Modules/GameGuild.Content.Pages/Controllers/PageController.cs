using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Content.Pages;

/// <summary>REST API controller for Page CRUD.</summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/pages")]
[Authorize]
public class PageController(IPageService pageService) : BaseApiController
{
    /// <summary>List pages with optional filtering.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PageDto>>> GetPages(
        [FromQuery] PageType? type = null,
        [FromQuery] PageStatus? status = null,
        [FromQuery] string? locale = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var pages = await pageService.GetPagesAsync(type, status, locale, parentId, skip, take).ConfigureAwait(false);
        return Ok(pages.ToDtos());
    }

    /// <summary>Get a page by ID (including sections).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PageDto>> GetPage(Guid id)
    {
        var page = await pageService.GetByIdAsync(id).ConfigureAwait(false);
        if (page is null) return NotFound();
        return Ok(page.ToDto());
    }

    /// <summary>Get a page by slug (including sections).</summary>
    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PageDto>> GetPageBySlug(string slug)
    {
        var page = await pageService.GetBySlugAsync(slug).ConfigureAwait(false);
        if (page is null) return NotFound();
        return Ok(page.ToDto());
    }

    /// <summary>Create a new page.</summary>
    [HttpPost]
    public async Task<ActionResult<PageDto>> CreatePage([FromBody] CreatePageDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var page = await pageService.CreateAsync(dto).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetPage), new { id = page.Id }, page.ToDto());
    }

    /// <summary>Update an existing page.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PageDto>> UpdatePage(Guid id, [FromBody] UpdatePageDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var page = await pageService.UpdateAsync(id, dto).ConfigureAwait(false);
        if (page is null) return NotFound();
        return Ok(page.ToDto());
    }

    /// <summary>Soft-delete a page.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeletePage(Guid id)
    {
        var deleted = await pageService.DeleteAsync(id).ConfigureAwait(false);
        if (!deleted) return NotFound();
        return NoContent();
    }

    /// <summary>Publish a page.</summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<PageDto>> Publish(Guid id)
    {
        // TODO: Extract user ID from claims when auth is wired
        var userId = Guid.Empty;
        var page = await pageService.PublishAsync(id, userId).ConfigureAwait(false);
        if (page is null) return NotFound();
        return Ok(page.ToDto());
    }

    /// <summary>Unpublish a page (back to Draft).</summary>
    [HttpPost("{id:guid}/unpublish")]
    public async Task<ActionResult<PageDto>> Unpublish(Guid id)
    {
        var page = await pageService.UnpublishAsync(id).ConfigureAwait(false);
        if (page is null) return NotFound();
        return Ok(page.ToDto());
    }

    // ──── Sections ────

    /// <summary>List sections for a page.</summary>
    [HttpGet("{pageId:guid}/sections")]
    public async Task<ActionResult<IEnumerable<PageSectionDto>>> GetSections(Guid pageId)
    {
        var sections = await pageService.GetSectionsAsync(pageId).ConfigureAwait(false);
        return Ok(sections.Select(s => s.ToDto()));
    }

    /// <summary>Create a section within a page.</summary>
    [HttpPost("{pageId:guid}/sections")]
    public async Task<ActionResult<PageSectionDto>> CreateSection(Guid pageId, [FromBody] CreatePageSectionDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var section = await pageService.CreateSectionAsync(pageId, dto).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetSection), new { pageId, sectionId = section.Id }, section.ToDto());
    }

    /// <summary>Get a specific section.</summary>
    [HttpGet("{pageId:guid}/sections/{sectionId:guid}")]
    public async Task<ActionResult<PageSectionDto>> GetSection(Guid pageId, Guid sectionId)
    {
        var section = await pageService.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
        if (section is null || section.PageId != pageId) return NotFound();
        return Ok(section.ToDto());
    }

    /// <summary>Update a section.</summary>
    [HttpPut("{pageId:guid}/sections/{sectionId:guid}")]
    public async Task<ActionResult<PageSectionDto>> UpdateSection(Guid pageId, Guid sectionId, [FromBody] UpdatePageSectionDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var section = await pageService.UpdateSectionAsync(sectionId, dto).ConfigureAwait(false);
        if (section is null || section.PageId != pageId) return NotFound();
        return Ok(section.ToDto());
    }

    /// <summary>Delete a section.</summary>
    [HttpDelete("{pageId:guid}/sections/{sectionId:guid}")]
    public async Task<ActionResult> DeleteSection(Guid pageId, Guid sectionId)
    {
        var existing = await pageService.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
        if (existing is null || existing.PageId != pageId) return NotFound();
        await pageService.DeleteSectionAsync(sectionId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Reorder sections within a page.</summary>
    [HttpPost("{pageId:guid}/sections/reorder")]
    public async Task<ActionResult> ReorderSections(Guid pageId, [FromBody] List<Guid> orderedIds)
    {
        await pageService.ReorderSectionsAsync(pageId, orderedIds).ConfigureAwait(false);
        return NoContent();
    }
}
