# Posts Module

Consolidated summary.

## Purpose

Emit contextual posts from domain events (user registration / profile updates / activation) plus manual posts.

## Event Handlers

- UserProfileCreated → welcome post
- UserProfileUpdated (significant fields) → activity post
- UserCreated / UserActivated → announcement posts

## Architecture

Handlers execute post-commit; failures logged (do not fail primary transaction).

## Adding an Event → Post Mapping

1. Define domain event
2. Implement handler (inject post service)
3. Map event → templated post
