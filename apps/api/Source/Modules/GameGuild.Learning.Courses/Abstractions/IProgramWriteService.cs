namespace GameGuild.Learning.Courses;

/// <summary>
/// Write-side operations for Programs: create, update, delete, clone,
/// content management, user management, progress mutations, monetization, and product integration.
/// </summary>
public interface IProgramWriteService
{
  // ── Program CRUD ────────────────────────────────────────────────────
  Task<Program> CreateProgramAsync(Program program);
  Task<Program> UpdateProgramAsync(Program program);
  Task DeleteProgramAsync(Guid id);
  Task<Program> CloneProgramAsync(Guid id, string newTitle);

  // ── Program CRUD with DTOs ──────────────────────────────────────────
  Task<Program> CreateProgramAsync(CreateProgramDto createDto);
  Task<Program?> UpdateProgramAsync(Guid id, UpdateProgramDto updateDto);

  // ── Content Management ──────────────────────────────────────────────
  Task<ProgramContent> AddContentAsync(Guid programId, ProgramContent content);
  Task<ProgramContent> UpdateContentAsync(ProgramContent content);
  Task DeleteContentAsync(Guid contentId);
  Task<Program> ReorderContentAsync(Guid programId, List<Guid> contentIds);
  Task<ProgramContent?> AddContentAsync(Guid programId, CreateContentDto contentDto);
  Task<ProgramContent?> UpdateContentAsync(Guid programId, Guid contentId, UpdateContentDto contentDto);
  Task<bool> RemoveContentAsync(Guid programId, Guid contentId);

  // ── User Management ─────────────────────────────────────────────────
  Task<ProgramUser> AddUserAsync(Guid programId, Guid userId);
  Task<ProgramUser> RemoveUserAsync(Guid programId, Guid userId);
  Task<UserProgressDto?> AddUserToProgramAsync(Guid programId, Guid userId);
  Task<bool> RemoveUserFromProgramAsync(Guid programId, Guid userId);

  // ── Progress Mutations ──────────────────────────────────────────────
  Task<Program> UpdateUserProgressAsync(Guid programId, Guid userId, Guid contentId, ProgressStatus status);
  Task<UserProgressDto?> UpdateUserProgressAsync(Guid programId, Guid userId, UpdateProgressDto progressDto);
  Task<ContentInteraction?> SubmitUserContentAsync(Guid programId, Guid userId, Guid contentId, string submissionData);
  Task<bool> MarkContentCompletedAsync(Guid programId, Guid userId, Guid contentId);
  Task<bool> ResetUserProgressAsync(Guid programId, Guid userId);

  // ── Monetization ────────────────────────────────────────────────────
  Task<Program?> EnableMonetizationAsync(Guid id, MonetizationDto monetizationDto);
  Task<Program?> DisableMonetizationAsync(Guid id);
  Task<PricingDto?> UpdateProgramPricingAsync(Guid id, UpdatePricingDto pricingDto);

  // ── Product Integration ─────────────────────────────────────────────
  Task<Guid?> CreateProductFromProgramAsync(Guid programId, CreateProductFromProgramDto productDto);
  Task<bool> LinkProgramToProductAsync(Guid programId, Guid productId);
  Task<bool> UnlinkProgramFromProductAsync(Guid programId, Guid productId);
}
