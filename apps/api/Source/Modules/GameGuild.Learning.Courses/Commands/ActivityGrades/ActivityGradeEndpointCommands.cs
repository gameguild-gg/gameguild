using GameGuild.CQRS;
using GameGuild.Learning.Grading.Contracts;

namespace GameGuild.Learning.Courses;

public sealed record GradeActivityEndpointCommand(
    Guid ContentInteractionId,
    Guid GraderProgramUserId,
    ScoreValue Points,
    ScoreValue MaxPoints,
    string? Feedback,
    string? GradingDetails) : ICommand<ActivityGrade>;

public sealed record UpdateActivityGradeEndpointCommand(
    Guid GradeId,
    ScoreValue? Points,
    ScoreValue? MaxPoints,
    string? Feedback,
    string? GradingDetails) : ICommand<ActivityGrade?>;

public sealed record DeleteActivityGradeEndpointCommand(Guid GradeId) : ICommand<bool>;

public sealed class ActivityGradeEndpointCommandHandler(IActivityGradeService activityGradeService) :
    ICommandHandler<GradeActivityEndpointCommand, ActivityGrade>,
    ICommandHandler<UpdateActivityGradeEndpointCommand, ActivityGrade?>,
    ICommandHandler<DeleteActivityGradeEndpointCommand, bool>
{
    public Task<ActivityGrade> Handle(GradeActivityEndpointCommand request, CancellationToken cancellationToken) =>
        activityGradeService.GradeActivityAsync(
            request.ContentInteractionId,
            request.GraderProgramUserId,
            request.Points,
            request.MaxPoints,
            request.Feedback,
            request.GradingDetails);

    public Task<ActivityGrade?> Handle(UpdateActivityGradeEndpointCommand request, CancellationToken cancellationToken) =>
        activityGradeService.UpdateGradeAsync(
            request.GradeId,
            request.Points,
            request.MaxPoints,
            request.Feedback,
            request.GradingDetails);

    public Task<bool> Handle(DeleteActivityGradeEndpointCommand request, CancellationToken cancellationToken) =>
        activityGradeService.DeleteGradeAsync(request.GradeId);
}
