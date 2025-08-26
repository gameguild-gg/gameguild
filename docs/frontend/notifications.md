# Notification Backend Integration

Consolidated summary.

## Data Sources

- Comments (isNotification flag)
- Achievements
- Activity posts

## Server Actions

`notifications.github.auth.actions.ts` fetches, transforms & caches (60s revalidation).

## Error Handling

Partial failures degrade gracefully; successful sources still render.
