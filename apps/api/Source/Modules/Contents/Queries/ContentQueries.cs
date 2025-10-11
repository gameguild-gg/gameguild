using System;
using System.Collections.Generic;
using GameGuild.CQRS;
using GameGuild.Modules.Contents.Models;

namespace GameGuild.Modules.Contents.Queries;

/// <summary>
/// CQRS Queries for Content Management
/// </summary>

// Get content by ID query
public record GetContentByIdQuery(
    Guid ContentId
) : IRequest<Result<Content?>>;

// Get content by slug query
public record GetContentBySlugQuery(
    string Slug
) : IRequest<Result<Content?>>;

// Get content by tenant query
public record GetContentByTenantQuery(
    Guid TenantId
) : IRequest<Result<IEnumerable<Content>>>;

// Get content by author query
public record GetContentByAuthorQuery(
    Guid AuthorId
) : IRequest<Result<IEnumerable<Content>>>;

// Get content by type query
public record GetContentByTypeQuery(
    ContentType Type
) : IRequest<Result<IEnumerable<Content>>>;

// Get content by status query
public record GetContentByStatusQuery(
    ContentStatus Status
) : IRequest<Result<IEnumerable<Content>>>;

// Search content query
public record SearchContentQuery(
    string SearchTerm,
    Guid? TenantId
) : IRequest<Result<IEnumerable<Content>>>;

// Get published content query
public record GetPublishedContentQuery(
    Guid? TenantId,
    ContentType? Type,
    int? Limit
) : IRequest<Result<IEnumerable<Content>>>;

// Get content versions query
public record GetContentVersionsQuery(
    Guid ContentId
) : IRequest<Result<IEnumerable<ContentVersion>>>;

// Get specific content version query
public record GetContentVersionQuery(
    Guid ContentId,
    int Version
) : IRequest<Result<ContentVersion?>>;

// Get scheduled content query
public record GetScheduledContentReadyToPublishQuery() : IRequest<Result<IEnumerable<Content>>>;
