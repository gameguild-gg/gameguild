using GameGuild.CQRS;

namespace GameGuild.Projects;

public sealed record ProjectStoreProductProjection(Guid LinkId, Guid ProjectId, Guid ProductId);

public sealed record LinkProjectStoreProductCommand(Guid ProjectId, Guid ProductId)
    : ICommand<Result<ProjectStoreProductProjection>>;

public sealed record UnlinkProjectStoreProductCommand(Guid ProjectId, Guid ProductId)
    : ICommand<Result<bool>>;

public sealed record GetProjectStoreProductsQuery(Guid ProjectId)
    : IQuery<Result<IReadOnlyList<ProjectStoreProductProjection>>>;

public sealed record GetPublicStoreProductProjectsQuery(Guid ProductId)
    : IQuery<Result<IReadOnlyList<ProjectStoreProductProjection>>>;

public sealed record LinkProjectStoreProductRequest(Guid ProductId);
