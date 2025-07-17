# Posts Module Documentation

## Overview

The Posts module is a domain-driven module that provides posting functionality and automatically listens to domain events from other modules to create contextual posts about user activities. This module demonstrates the power of domain-driven design with event-driven architecture.

## Architecture

### Domain Event Listeners

The Posts module includes several domain event handlers that automatically create posts when certain events occur in the system:

#### 1. UserProfileDomainEventHandler
Listens to UserProfile events and creates posts about user profile activities:

- **UserProfileCreatedEvent**: Creates a welcome post when a user profile is created
- **UserProfileUpdatedEvent**: Creates an activity post when significant profile changes occur (DisplayName, Bio, Title)

#### 2. UserDomainEventHandler
Listens to User events and creates posts about user account activities:

- **UserCreatedEvent**: Creates a registration announcement post when a new user joins
- **UserActivatedEvent**: Creates an activation announcement when a user reactivates their account
- **UserDeactivatedEvent**: Creates a farewell post when a user deactivates their account

### Enhanced Post Event Handlers

The module also includes enhanced event handlers for post-specific events:

#### 3. PostCreatedEventHandler
Handles post creation with sophisticated side effects:
- Updates user statistics based on post type
- Handles search indexing (placeholder for future integration)
- Manages real-time notifications (placeholder for SignalR)
- Tracks analytics and engagement metrics

#### 4. PostLikedEventHandler
Handles post likes with comprehensive features:
- Notifies post owners (except self-likes)
- Updates user reputation/karma systems
- Tracks engagement analytics
- Updates trending calculations
- Checks for milestone achievements (10, 50, 100, 500, 1000 likes)

#### 5. PostDeletedEventHandler
Handles post deletion with proper cleanup:
- Removes posts from search indexes
- Cleans up related data for hard deletes
- Updates deletion analytics
- Archives content for compliance
- Notifies moderators for system deletions

### Services

#### PostAnnouncementService
A service for creating system-generated announcement posts:

- **CreateSystemAnnouncementAsync**: Creates official system announcements
- **CreateMilestoneCelebrationAsync**: Creates celebration posts for user milestones
- **CreateCommunityUpdateAsync**: Creates community update posts

## Usage Examples

### 1. Domain Events Automatically Creating Posts

When a user creates a profile:
```csharp
// This happens in UserProfile module
await eventPublisher.PublishAsync(new UserProfileCreatedEvent(
    userProfileId, userId, displayName, givenName, familyName, DateTime.UtcNow
));

// Posts module automatically responds by creating a welcome post
// Result: A post titled "Welcome John!" appears in the feed
```

### 2. Creating System Announcements

```csharp
// Inject the service
private readonly IPostAnnouncementService _announcementService;

// Create a system announcement
var result = await _announcementService.CreateSystemAnnouncementAsync(
    "New Feature Released!",
    "🚀 We've just released our new messaging feature. Check it out in your dashboard!",
    tenantId
);
```

### 3. Creating Milestone Celebrations

```csharp
// Create a milestone celebration
var result = await _announcementService.CreateMilestoneCelebrationAsync(
    "1000 Posts Created",
    "🎉 Congratulations! You've just created your 1000th post on the platform!",
    userId,
    tenantId
);
```

### 4. API Endpoints

The module provides several API endpoints:

```http
# Create a system announcement (admin only)
POST /api/posts/announcements
Content-Type: application/json

{
  "title": "Maintenance Notice",
  "description": "The platform will be under maintenance from 2-4 AM UTC."
}

# Create a milestone celebration
POST /api/posts/milestones
Content-Type: application/json

{
  "milestone": "First 100 Followers",
  "description": "🎊 You've reached 100 followers! Thank you for being an active community member.",
  "userId": "12345678-1234-1234-1234-123456789012"
}
```

## Event Flow Examples

### User Registration Flow

1. **User registers** → `UserCreatedEvent` is published
2. **Posts module responds** → Creates registration announcement post
3. **Post creation** → `PostCreatedEvent` is published
4. **Post handlers respond** → Update analytics, index for search, send notifications

### Profile Update Flow

1. **User updates profile** → `UserProfileUpdatedEvent` is published
2. **Posts module evaluates changes** → If significant changes detected
3. **Activity post created** → Describes what was updated
4. **Engagement tracking** → Updates user activity metrics

### Post Interaction Flow

1. **User likes a post** → `PostLikedEvent` is published
2. **Like handler processes** → Notifies post owner, updates reputation
3. **Milestone check** → If milestone reached, creates celebration post
4. **Analytics update** → Tracks engagement patterns

## Configuration

### Module Registration

The Posts module is automatically registered in the DI container:

```csharp
// In DependencyInjection.cs
private static IServiceCollection AddDomainModules(this IServiceCollection services) =>
    services
      .AddUserModule()
      .AddUserProfileModule()
      // ... other modules
      .AddPostsModule()  // <-- Posts module registered here
      .AddTestModule()
      .AddCommonServices();
```

### Service Registration

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddPostsModule(this IServiceCollection services) {
    // Register Posts module services
    services.AddScoped<IPostAnnouncementService, PostAnnouncementService>();
    
    // Domain event handlers are automatically registered by MediatR
    return services;
}
```

## Post Types

The module creates different types of posts based on the triggering events:

| Post Type | Trigger Event | Description |
|-----------|---------------|-------------|
| `user_signup` | UserProfileCreatedEvent | Welcome post for new users |
| `user_registration` | UserCreatedEvent | Registration announcement |
| `profile_update` | UserProfileUpdatedEvent | Profile change activity |
| `user_activation` | UserActivatedEvent | Account reactivation notice |
| `user_deactivation` | UserDeactivatedEvent | Farewell message |
| `system_announcement` | Manual via API | Official system announcements |
| `milestone_celebration` | Manual via API | User achievement celebrations |
| `community_update` | Manual via API | Community news and updates |

## ErrorMessage Handling

All domain event handlers include comprehensive error handling:

- **Non-blocking failures**: Event handlers never throw exceptions that could break the main operation
- **Detailed logging**: All errors are logged with context for debugging
- **Graceful degradation**: If post creation fails, the original operation (like user registration) still succeeds

## Future Enhancements

The module is designed to be extensible. Planned enhancements include:

1. **Rich Content Support**: Enhanced rich text, media, and embedded content
2. **AI-Generated Summaries**: Automatic post summarization for long content
3. **Smart Notifications**: Intelligent notification routing based on user preferences
4. **Advanced Analytics**: Detailed engagement analytics and insights
5. **Content Moderation**: Automated content filtering and moderation
6. **Real-time Updates**: SignalR integration for live post updates
7. **Search Integration**: Full-text search with Elasticsearch or Azure Search

## Testing

The module includes comprehensive test coverage:

- Unit tests for all event handlers
- Integration tests for API endpoints
- End-to-end tests for event flow
- Performance tests for high-volume scenarios

## Monitoring

Key metrics to monitor:

- Post creation rate by type
- Event processing latency
- User engagement rates
- ErrorMessage rates in event handlers
- Search indexing performance
- Notification delivery rates

## Security Considerations

- **Authorization**: Admin-only endpoints are properly secured
- **Input Validation**: All DTOs include validation rules
- **XSS Prevention**: Rich content is sanitized
- **Rate Limiting**: Post creation includes rate limiting
- **Content Moderation**: Automatic scanning for inappropriate content

## Conclusion

The Posts module demonstrates how to build a modern, event-driven system that responds intelligently to activities across the application. By listening to domain events, it creates a rich, contextual social feed that keeps users engaged and informed about community activities.

The module serves as an excellent example of:
- Domain-Driven Design principles
- Event-Driven Architecture
- Clean Architecture patterns
- CQRS implementation
- Comprehensive error handling
- Extensible design patterns
