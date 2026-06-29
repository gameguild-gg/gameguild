# GameGuild Component Package Inventory

This inventory classifies app-owned components before moving reusable components into context packages. The goal is to reuse components across `apps/web` and `apps/learning` without turning `@game-guild/ui` into a domain-component dumping ground.

## Dependency Direction

Allowed:

```text
apps/web, apps/learning
  -> packages/features/*
  -> @game-guild/ui
```

Not allowed:

```text
@game-guild/ui -> packages/features/*
packages/features/* -> apps/web or apps/learning
```

## Existing Feature Packages

- `packages/features/analytics`
- `packages/features/community-members`
- `packages/features/cookies`
- `packages/features/courses`
- `packages/features/errors`
- `packages/features/web3`

## Reusable Context Package Candidates

### `@game-guild/block-content-editor`

Move reusable editor implementation from `apps/web/src/components/block-content-editor` to `packages/features/block-content-editor`.

Move:

- `apps/web/src/components/block-content-editor/docs`
- `apps/web/src/components/block-content-editor/embed`
- `apps/web/src/components/block-content-editor/engines`
- `apps/web/src/components/block-content-editor/extras`
- `apps/web/src/components/block-content-editor/hooks`
- `apps/web/src/components/block-content-editor/lexical-surface`
- `apps/web/src/components/block-content-editor/lib`
- `apps/web/src/components/block-content-editor/nodes`
- `apps/web/src/components/block-content-editor/plugins`
- `apps/web/src/components/block-content-editor/services`
- `apps/web/src/components/block-content-editor/utils`
- `apps/web/src/components/block-content-editor/lazy-client-components.tsx`
- `apps/web/src/components/block-content-editor/theme-provider.tsx`
- `apps/web/src/components/block-content-editor/youtube-audio-style.tsx`

Keep app-local unless refactored into configurable shell components:

- `apps/web/src/components/block-content-editor/top-menu.tsx`

Reason: `top-menu.tsx` is route/navigation branded and imports app routing concerns.

### `@game-guild/courses`

Expand the existing package and move reusable course, catalog, course landing, learning viewer, module, assessment-facing, and course editor presentation components.

Move after replacing app aliases with package or UI imports:

- `apps/web/src/components/courses/common`
- `apps/web/src/components/courses/course`
- `apps/web/src/components/courses/learning`
- `apps/web/src/components/courses/tracks`
- `apps/web/src/components/courses/sections`
- Pure `.tsx` files under `apps/web/src/components/courses`

Keep app-local until decoupled:

- `apps/web/src/components/courses/actions.ts`
- `apps/web/src/components/courses/editor/actions.ts`
- Course components that directly import those server actions.

Reason: server actions and app session/cache code belong in app or app lib layers. Package components should receive data and callbacks through props.

### `@game-guild/auth-components`

Candidate package for duplicated app auth UI.

Potential sources:

- `apps/web/src/components/login-form.tsx`
- `apps/web/src/components/sign-in-form.tsx`
- `apps/web/src/components/signup-form.tsx`
- `apps/web/src/components/forgot-password-form.tsx`
- `apps/web/src/components/input-otp-form.tsx`
- `apps/learning/src/components/sign-in-form.tsx`
- `apps/learning/src/components/sign-up-form.tsx`

Requirement before moving: forms must accept route, copy, provider, and submit callbacks through props. They must not hard-code app routes or NextAuth behavior.

### `@game-guild/content-rendering`

Candidate package for markdown and lightweight content renderers shared by website, learning, course content, and block editor previews.

Potential sources:

- `apps/web/src/components/markdown-renderer`
- `apps/learning/src/components/markdown-renderer.tsx`
- content renderer variants found in `.tmp/gameguild-main` and `.tmp/gameguild-block-content-editor`

Keep this separate from `@game-guild/block-content-editor`; rendering published content is a broader context than editing content.

### `@game-guild/platform-shell`

Candidate package only after app-specific auth/navigation is injected.

Potential sources:

- `apps/web/src/components/layout/dashboard-shell.tsx`
- `apps/web/src/components/layout/dashboard-header.tsx`
- `apps/web/src/components/layout/dashboard-sidebar.tsx`
- `apps/web/src/components/layout/dashboard-command-palette.tsx`

Keep app-local now:

- `apps/web/src/components/layout`

Reason: current dashboard layout imports app navigation, dashboard notification data, user menu/session behavior, and route-specific state. It should be refactored to receive navigation trees, user state, notification state, and callbacks before moving.

### `@game-guild/public-site`

Candidate package only if public website chrome is reused across apps.

Keep app-local now:

- `apps/web/src/components/site/public-website-shell.tsx`
- `apps/web/src/components/site/public-website-nav.tsx`

Reason: current shell imports app auth and localized app routing. It can become reusable later by receiving auth state, nav links, and routing functions as props.

### Future Product Context Packages From Main/Temp Sources

Use these when migrating older `main` implementation components into the new architecture:

- `@game-guild/projects`
- `@game-guild/programs`
- `@game-guild/testing-lab`
- `@game-guild/launch-pad`
- `@game-guild/community-feed`
- `@game-guild/notifications`

Each package should be introduced only with a clear consumer and app-agnostic props.

## Primitive UI

True primitives belong in `@game-guild/ui`, not feature packages:

- shadcn/Radix wrappers such as button, dialog, dropdown, table, tabs, sheet, select, input, form, toast/sonner, sidebar, tooltip.
- generic primitive helpers such as `cn`.

Current app-local primitives under `apps/web/src/components/ui` should either be replaced with imports from `@game-guild/ui/components/*` or moved to `@game-guild/ui` if they do not already exist there.

Do not move these to `@game-guild/ui` without review:

- `apps/web/src/components/ui/github-issue-modal.tsx`
- `apps/web/src/components/ui/content-edit-menu.tsx`

Reason: they are product workflows, not primitives.

## Must Stay App-Owned

These categories should not move to feature packages:

- Next route files under `apps/*/src/app/**`
- API route handlers
- server actions with `"use server"`
- files importing `next/headers`, `next/cookies`, `next/cache`, or app-local auth/session code
- app provider composition
- deployment/runtime config
- route-specific metadata and localized route wrappers

## Main Risks

- Block editor has many app alias imports such as `@/components/block-content-editor/*`; these must become package-local or `@game-guild/block-content-editor/*` imports.
- Course components import `@/components/ui/*`, `@/components/common/*`, app route helpers, and app server actions. UI imports can move to `@game-guild/ui`; server actions must stay app-owned.
- Moving dashboard/public shell code too early would spread app-specific auth and route assumptions into packages.
- Tests should move with reusable components, but route/page tests should remain in the app.
