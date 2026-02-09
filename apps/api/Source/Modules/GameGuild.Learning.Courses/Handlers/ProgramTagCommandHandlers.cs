using GameGuild.CQRS;
using GameGuild.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

public sealed class AddTagToProgramCommandHandler(
    IApplicationDbContext context,
    ILogger<AddTagToProgramCommandHandler> logger)
    : ICommandHandler<AddTagToProgramCommand, ProgramTag>
{
    public async Task<ProgramTag> Handle(AddTagToProgramCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Adding tag {TagId} to program {ProgramId} with proficiency {Proficiency}",
            request.TagId, request.ProgramId, request.ProficiencyLevel);

        // Verify program exists
        var program = await context.Set<Program>()
            .FirstOrDefaultAsync(p => p.Id == request.ProgramId && p.DeletedAt == null, cancellationToken).ConfigureAwait(false);

        if (program == null)
        {
            throw new InvalidOperationException($"Program {request.ProgramId} not found");
        }

        // Verify tag exists
        var tag = await context.Set<Tag>()
            .FirstOrDefaultAsync(t => t.Id == request.TagId && t.IsActive, cancellationToken).ConfigureAwait(false);

        if (tag == null)
        {
            throw new InvalidOperationException($"Tag {request.TagId} not found or inactive");
        }

        // Check if already tagged
        var existingTag = await context.Set<ProgramTag>()
            .FirstOrDefaultAsync(pt => pt.ProgramId == request.ProgramId && pt.TagId == request.TagId, cancellationToken).ConfigureAwait(false);

        if (existingTag != null)
        {
            throw new InvalidOperationException($"Program {request.ProgramId} is already tagged with {request.TagId}");
        }

        var programTag = ProgramTag.Create(
            programId: request.ProgramId,
            tagId: request.TagId,
            proficiencyLevel: request.ProficiencyLevel,
            isPrimary: request.IsPrimary,
            displayOrder: request.DisplayOrder);

        context.Set<ProgramTag>().Add(programTag);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Added tag {TagId} to program {ProgramId}", request.TagId, request.ProgramId);
        return programTag;
    }
}

public sealed class UpdateProgramTagCommandHandler(
    IApplicationDbContext context,
    ILogger<UpdateProgramTagCommandHandler> logger)
    : ICommandHandler<UpdateProgramTagCommand, ProgramTag>
{
    public async Task<ProgramTag> Handle(UpdateProgramTagCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating tag {TagId} on program {ProgramId}", request.TagId, request.ProgramId);

        var programTag = await context.Set<ProgramTag>()
            .FirstOrDefaultAsync(pt => pt.ProgramId == request.ProgramId && pt.TagId == request.TagId, cancellationToken).ConfigureAwait(false);

        if (programTag == null)
        {
            throw new InvalidOperationException($"Program {request.ProgramId} is not tagged with {request.TagId}");
        }

        if (request.ProficiencyLevel.HasValue)
        {
            programTag.UpdateProficiency(request.ProficiencyLevel.Value);
        }

        if (request.IsPrimary.HasValue)
        {
            programTag.SetPrimary(request.IsPrimary.Value);
        }

        if (request.DisplayOrder.HasValue)
        {
            programTag.SetDisplayOrder(request.DisplayOrder.Value);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return programTag;
    }
}

public sealed class RemoveTagFromProgramCommandHandler(
    IApplicationDbContext context,
    ILogger<RemoveTagFromProgramCommandHandler> logger)
    : ICommandHandler<RemoveTagFromProgramCommand>
{
    public async Task<Unit> Handle(RemoveTagFromProgramCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing tag {TagId} from program {ProgramId}", request.TagId, request.ProgramId);

        var programTag = await context.Set<ProgramTag>()
            .FirstOrDefaultAsync(pt => pt.ProgramId == request.ProgramId && pt.TagId == request.TagId, cancellationToken).ConfigureAwait(false);

        if (programTag == null)
        {
            logger.LogWarning("Tag {TagId} not found on program {ProgramId}", request.TagId, request.ProgramId);
            return Unit.Value;
        }

        context.Set<ProgramTag>().Remove(programTag);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Removed tag {TagId} from program {ProgramId}", request.TagId, request.ProgramId);
        return Unit.Value;
    }
}

public sealed class BulkAddTagsToProgramCommandHandler(
    IApplicationDbContext context,
    ILogger<BulkAddTagsToProgramCommandHandler> logger)
    : ICommandHandler<BulkAddTagsToProgramCommand, IEnumerable<ProgramTag>>
{
    public async Task<IEnumerable<ProgramTag>> Handle(BulkAddTagsToProgramCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Bulk adding {Count} tags to program {ProgramId}", request.Tags.Count(), request.ProgramId);

        // Verify program exists
        var program = await context.Set<Program>()
            .FirstOrDefaultAsync(p => p.Id == request.ProgramId && p.DeletedAt == null, cancellationToken).ConfigureAwait(false);

        if (program == null)
        {
            throw new InvalidOperationException($"Program {request.ProgramId} not found");
        }

        // Get existing tags to avoid duplicates
        var existingTagIds = await context.Set<ProgramTag>()
            .Where(pt => pt.ProgramId == request.ProgramId)
            .Select(pt => pt.TagId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var newTags = new List<ProgramTag>();

        foreach (var dto in request.Tags)
        {
            if (existingTagIds.Contains(dto.TagId))
            {
                logger.LogDebug("Skipping duplicate tag {TagId}", dto.TagId);
                continue;
            }

            var programTag = ProgramTag.Create(
                programId: request.ProgramId,
                tagId: dto.TagId,
                proficiencyLevel: dto.ProficiencyLevel,
                isPrimary: dto.IsPrimary,
                displayOrder: dto.DisplayOrder);

            newTags.Add(programTag);
        }

        if (newTags.Any())
        {
            context.Set<ProgramTag>().AddRange(newTags);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Added {Count} new tags to program {ProgramId}", newTags.Count, request.ProgramId);
        return newTags;
    }
}

public sealed class ReorderProgramTagsCommandHandler(
    IApplicationDbContext context,
    ILogger<ReorderProgramTagsCommandHandler> logger)
    : ICommandHandler<ReorderProgramTagsCommand>
{
    public async Task<Unit> Handle(ReorderProgramTagsCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Reordering tags for program {ProgramId}", request.ProgramId);

        var programTags = await context.Set<ProgramTag>()
            .Where(pt => pt.ProgramId == request.ProgramId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var order = 0;
        foreach (var tagId in request.TagIdsInOrder)
        {
            var tag = programTags.FirstOrDefault(pt => pt.TagId == tagId);
            if (tag != null)
            {
                tag.SetDisplayOrder(order++);
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Reordered tags for program {ProgramId}", request.ProgramId);
        return Unit.Value;
    }
}
