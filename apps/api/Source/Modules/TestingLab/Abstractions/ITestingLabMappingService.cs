namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary>
/// Service for mapping between entities and DTOs in the Testing Lab module
/// </summary>
public interface ITestingLabMappingService
{
    // Testing Request Mappings
    TestingRequest MapToEntity(CreateTestingRequestDto dto);
    TestingRequest MapToEntity(UpdateTestingRequestDto dto, TestingRequest existing);
    CreateTestingRequestDto MapToCreateDto(TestingRequest entity);
    UpdateTestingRequestDto MapToUpdateDto(TestingRequest entity);

    // Testing Session Mappings
    TestingSession MapToEntity(CreateTestingSessionDto dto);
    CreateTestingSessionDto MapToCreateDto(TestingSession entity);

    // Feedback Mappings
    TestingFeedback MapToEntity(SubmitFeedbackDto dto);
    SubmitFeedbackDto MapToDto(TestingFeedback entity);

    // Settings Mappings
    TestingLabSettings MapToEntity(TestingLabSettingsDto dto);
    TestingLabSettingsDto MapToDto(TestingLabSettings entity);

    // Collection Mappings
    IEnumerable<TTarget> MapCollection<TSource, TTarget>(
        IEnumerable<TSource> source, 
        Func<TSource, TTarget> mapper);

    // Validation
    bool ValidateMapping<TSource, TTarget>(TSource source, TTarget target);
}
