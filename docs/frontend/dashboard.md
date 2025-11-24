# Dashboard Integration & Organization

Consolidated summary.

## Architecture

- Next.js 15 server actions for CRUD
- SSR-first pages; client components only when needed
- Feature-based `/src/features/*` layout

## Server Action Pattern

Co-located `[feature].github.auth.actions.ts` per domain.

## Caching

Cache tags + selective revalidation after mutations.

## Features

- Programs / Products / Achievements dashboards
- Activity feed (posts + users)
- Refresh coordination action

## Improvements

- Removed monolithic action file
- Centralized feature index exports
