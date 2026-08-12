using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

public sealed class CodingAssignmentContentService(
    IApplicationDbContext context,
    IValidator<CodingAssignmentContent> validator,
    IAssessmentGradingSync assessmentGradingSync) : ICodingAssignmentContentService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new();

    public async Task<CodingAssignmentContent?> GetPublicAsync(
        Guid programId,
        Guid contentId,
        Guid userId,
        CancellationToken ct = default)
    {
        var content = await GetContentAsync(programId, contentId, ct).ConfigureAwait(false);
        if (content == null) return null;

        var parsed = Parse(content.JsonBody);
        if (parsed == null) return null;

        return StripPrivate(parsed);
    }

    public async Task<CodingAssignmentContent?> GetFullAsync(
        Guid programId,
        Guid contentId,
        CancellationToken ct = default)
    {
        var content = await GetContentAsync(programId, contentId, ct).ConfigureAwait(false);
        return content == null ? null : Parse(content.JsonBody);
    }

    public async Task<Result<CodingAssignmentContent>> UpsertAsync(
        Guid programId,
        Guid contentId,
        CodingAssignmentContent body,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(body, ct).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            var code = validation.Errors[0].ErrorCode;
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<CodingAssignmentContent>(
                Error.Validation(string.IsNullOrEmpty(code) ? "CodingAssignmentContent.Invalid" : code, message));
        }

        var content = await GetContentAsync(programId, contentId, ct).ConfigureAwait(false);
        if (content == null)
        {
            return Result.Failure<CodingAssignmentContent>(
                Error.NotFound("ProgramContent", "Program content not found for this program."));
        }

        content.JsonBody = JsonSerializer.Serialize(body, s_jsonOptions);
        content.Touch();
        context.Set<ProgramContent>().Update(content);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        await assessmentGradingSync.SyncAsync(contentId, body.Grading.MaxScore, body.Grading.PassingScore, ct).ConfigureAwait(false);

        return Result.Success(body);
    }

    private async Task<ProgramContent?> GetContentAsync(Guid programId, Guid contentId, CancellationToken ct)
    {
        return await context.Set<ProgramContent>()
            .AsTracking()
            .FirstOrDefaultAsync(pc => pc.Id == contentId
                                        && pc.ProgramId == programId
                                        && pc.DeletedAt == null, ct)
            .ConfigureAwait(false);
    }

    private static CodingAssignmentContent? Parse(string? jsonBody)
    {
        if (string.IsNullOrWhiteSpace(jsonBody)) return null;
        try
        {
            return JsonSerializer.Deserialize<CodingAssignmentContent>(jsonBody, s_jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static CodingAssignmentContent StripPrivate(CodingAssignmentContent source)
    {
        var publicFiles = source.Data.Files
            .Where(kv => kv.Value.Visibility == "Public")
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return source with
        {
            Data = source.Data with { Files = publicFiles },
            Tests = source.Tests with { Private = new List<Test>() }
        };
    }
}
