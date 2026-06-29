# Context Component Packages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move reusable GameGuild domain components out of `apps/web` and `apps/learning` into context packages under `packages/features/*`, while keeping `@game-guild/ui` limited to primitive shared UI and preserving a clean path for components recovered from the older `main` and block-editor branches.

**Architecture:** Apps own routes, layouts, server actions, API routes, data loading, auth/session wiring, and deployment configuration. Feature packages own reusable React components, hooks, pure helpers, and context-specific types for a single product area. Dependency flow is `apps/* -> packages/features/* -> @game-guild/ui`; `@game-guild/ui` must never import a feature package.

**Tech Stack:** pnpm workspaces, Next.js 16, React 19, TypeScript, Vitest, Playwright, Turbopack, existing `@game-guild/typescript-config`, existing `@game-guild/eslint-config`, existing `@game-guild/ui`.

---

## Package Boundary Rules

- Keep primitive, unbranded controls in `packages/infrastructure/ui` as `@game-guild/ui`: buttons, dialogs, menus, inputs, cards, fields, tabs, tooltips, tables, layout primitives, icons wrappers, and form primitives.
- Put course catalog, professor course management, course delivery, modules, lessons, assessments, grading groups, and course landing components in `packages/features/courses` as `@game-guild/courses`.
- Put the imported block editor in `packages/features/block-content-editor` as `@game-guild/block-content-editor`.
- Keep public website route composition, metadata, server-side fetches, and page-level layouts in `apps/web/src/app`.
- Keep learning app route composition, session checks, and app-specific layouts in `apps/learning/src/app`.
- Do not move API route handlers, server actions with `"use server"`, Next route files, cookies/session code, database access, or deployment/runtime config into feature component packages.
- Keep React Compiler enabled. Fix package code rather than disabling compiler checks.
- Move one context package at a time and commit after each verified package migration.
- Use `docs/component-package-inventory.md` as the source of truth for what moves now, what remains app-owned, and what should be migrated from `.tmp` branches later.
- New context packages should be introduced only when there is a real reusable context. Do not create packages for route-only screens.

## Target File Structure

Create or modify these files during execution:

- Create: `packages/features/block-content-editor/package.json`
- Create: `packages/features/block-content-editor/tsconfig.json`
- Create: `packages/features/block-content-editor/src/index.ts`
- Create: `packages/features/block-content-editor/src/components/*`
- Create: `packages/features/block-content-editor/src/docs/*`
- Create: `packages/features/block-content-editor/src/embed/*`
- Create: `packages/features/block-content-editor/src/engines/*`
- Create: `packages/features/block-content-editor/src/extras/*`
- Create: `packages/features/block-content-editor/src/hooks/*`
- Create: `packages/features/block-content-editor/src/lexical-surface/*`
- Create: `packages/features/block-content-editor/src/lib/*`
- Create: `packages/features/block-content-editor/src/nodes/*`
- Create: `packages/features/block-content-editor/src/plugins/*`
- Create: `packages/features/block-content-editor/src/services/*`
- Create: `packages/features/block-content-editor/src/utils/*`
- Modify: `packages/features/courses/package.json`
- Modify: `packages/features/courses/tsconfig.json`
- Modify: `packages/features/courses/src/index.ts`
- Create: `packages/features/courses/src/components/*`
- Create: `packages/features/courses/src/actions.ts` only if the current file is client-safe and has no `"use server"`
- Modify: `apps/web/package.json`
- Modify: `apps/web/next.config.ts`
- Modify: `apps/web/src/app/[locale]/(block-content-editor)/**`
- Modify: `apps/web/src/app/[locale]/(dashboard)/dashboard/learning/**`
- Modify: `apps/web/src/app/[locale]/(dashboard)/dashboard/courses/**`
- Modify: `apps/web/src/app/[locale]/(dashboard)/dashboard/launch-pad/**` only where imports point to moved components
- Modify: `apps/web/src/app/[locale]/(dashboard)/dashboard/testing-lab/**` only where imports point to moved components
- Modify: `apps/learning/package.json`
- Modify: `apps/learning/next.config.ts` or `apps/learning/next.config.mjs`, whichever exists in this repo
- Test: existing tests moved with the migrated components
- Test: `apps/web/src/components/block-content-editor.migration.test.ts`
- Test: `apps/web/src/lib/__tests__/web-runtime-hardening.test.ts`
- Create: `docs/component-package-inventory.md`
- Create when needed: `packages/features/auth-components/package.json`
- Create when needed: `packages/features/content-rendering/package.json`
- Create later after decoupling: `packages/features/platform-shell/package.json`
- Create later after decoupling: `packages/features/public-site/package.json`
- Create later when migrating old main product components: `packages/features/projects/package.json`
- Create later when migrating old main product components: `packages/features/programs/package.json`
- Create later when migrating old main product components: `packages/features/testing-lab/package.json`
- Create later when migrating old main product components: `packages/features/launch-pad/package.json`
- Create later when migrating old main product components: `packages/features/community-feed/package.json`
- Create later when migrating old main product components: `packages/features/notifications/package.json`

## Task 1: Baseline Inventory And Safety Checks

**Files:**
- Read: `apps/web/src/components/courses`
- Read: `apps/web/src/components/block-content-editor`
- Read: `apps/web/next.config.ts`
- Read: `apps/learning/package.json`
- Read: `apps/learning/next.config.ts` or `apps/learning/next.config.mjs`
- Create: `docs/component-package-inventory.md`
- Read: `.tmp/gameguild-main/apps/web/src/components`
- Read: `.tmp/gameguild-block-content-editor/apps/web/src/components`

- [ ] **Step 1: Record the current domain component inventory**

Run:

```powershell
@(
  "apps/web/src/components/courses",
  "apps/web/src/components/block-content-editor",
  "apps/web/src/components/site",
  "apps/web/src/components/layout",
  "apps/learning/src/components"
) | ForEach-Object {
  if (Test-Path $_) {
    Write-Output "## $_"
    rg --files $_
  }
} | Set-Content docs/component-package-inventory.md
```

Expected: `docs/component-package-inventory.md` lists all current app-local component files for the migration.

- [ ] **Step 1b: Classify every component context**

Ensure `docs/component-package-inventory.md` includes these sections:

```markdown
## Reusable Context Package Candidates
## Primitive UI
## Must Stay App-Owned
## Future Product Context Packages From Main/Temp Sources
## Main Risks
```

Expected: each app component context is classified before any code moves.

- [ ] **Step 2: Capture app-local imports that must be removed**

Run:

```powershell
rg -n "@/components/(courses|block-content-editor)|components/(courses|block-content-editor)" apps/web/src apps/learning/src
```

Expected: output lists current import sites. Save the output in the execution notes for Task 2 and Task 3.

- [ ] **Step 3: Run the current focused verification before moving files**

Run:

```powershell
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
pnpm --filter @game-guild/web test -- src/components/block-content-editor.migration.test.ts src/lib/__tests__/web-runtime-hardening.test.ts
pnpm --filter @game-guild/web test:browser:block-editor
```

Expected: all commands pass before any package migration starts.

- [ ] **Step 4: Commit the inventory document**

Run:

```powershell
git add docs/component-package-inventory.md
git commit -m "docs: inventory reusable frontend components"
```

Expected: one docs-only commit records the starting point.

## Task 2: Add The Block Content Editor Feature Package Shell

**Files:**
- Create: `packages/features/block-content-editor/package.json`
- Create: `packages/features/block-content-editor/tsconfig.json`
- Create: `packages/features/block-content-editor/src/index.ts`
- Modify: `apps/web/package.json`
- Modify: `apps/web/next.config.ts`

- [ ] **Step 1: Create the package manifest**

Create `packages/features/block-content-editor/package.json` with:

```json
{
  "name": "@game-guild/block-content-editor",
  "version": "0.1.0",
  "type": "module",
  "description": "Reusable block content editor components and helpers for GameGuild apps",
  "license": "UNLICENSED",
  "private": true,
  "exports": {
    ".": "./src/index.ts",
    "./components/*": "./src/components/*.tsx",
    "./docs/*": "./src/docs/*",
    "./embed/*": "./src/embed/*",
    "./engines/*": "./src/engines/*",
    "./extras/*": "./src/extras/*",
    "./hooks/*": "./src/hooks/*",
    "./lexical-surface/*": "./src/lexical-surface/*",
    "./lib/*": "./src/lib/*",
    "./nodes/*": "./src/nodes/*",
    "./plugins/*": "./src/plugins/*",
    "./services/*": "./src/services/*",
    "./utils/*": "./src/utils/*"
  },
  "scripts": {
    "typecheck": "tsc --noEmit --pretty false"
  },
  "dependencies": {
    "@game-guild/ui": "workspace:*",
    "@lexical/react": "^0.31.0",
    "dompurify": "^3.4.10",
    "lexical": "^0.31.0",
    "lucide-react": "^0.525.0",
    "next": "16.2.9",
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  },
  "devDependencies": {
    "@game-guild/eslint-config": "workspace:*",
    "@game-guild/prettier-config": "workspace:*",
    "@game-guild/typescript-config": "workspace:*",
    "@types/node": "^20.0.0",
    "@types/react": "^19.0.0",
    "@types/react-dom": "^19.0.0",
    "eslint": "^9.0.0",
    "typescript": "^5.5.0"
  }
}
```

- [ ] **Step 2: Create the package TypeScript config**

Create `packages/features/block-content-editor/tsconfig.json` with:

```json
{
  "$schema": "https://json.schemastore.org/tsconfig",
  "extends": "@game-guild/typescript-config/tsconfig.react.json",
  "display": "Block Content Editor",
  "compilerOptions": {
    "baseUrl": "./",
    "paths": {
      "@game-guild/block-content-editor": [
        "./src/index.ts"
      ],
      "@game-guild/block-content-editor/*": [
        "./src/*"
      ]
    }
  },
  "include": [
    "./src"
  ],
  "files": [],
  "references": [],
  "exclude": [
    "node_modules",
    "dist"
  ]
}
```

- [ ] **Step 3: Create the package entry point**

Create `packages/features/block-content-editor/src/index.ts` with:

```ts
export {};
```

This intentionally exports nothing until the files move in Task 4. It lets the workspace resolve the package before the bulk migration.

- [ ] **Step 4: Add the workspace dependency to the web app**

Modify `apps/web/package.json` dependencies to include:

```json
"@game-guild/block-content-editor": "workspace:*"
```

Keep existing dependencies unchanged.

- [ ] **Step 5: Transpile the new package in the web app**

Modify `apps/web/next.config.ts`. If the file already has `transpilePackages`, include `@game-guild/block-content-editor` in the existing array. The final array must contain:

```ts
transpilePackages: [
  '@game-guild/ui',
  '@game-guild/courses',
  '@game-guild/block-content-editor',
],
```

If the existing file contains more packages, keep them and add `@game-guild/block-content-editor`.

- [ ] **Step 6: Verify the empty package shell**

Run:

```powershell
pnpm install --lockfile-only
pnpm --filter @game-guild/block-content-editor typecheck
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
```

Expected: all commands pass.

- [ ] **Step 7: Commit the package shell**

Run:

```powershell
git add packages/features/block-content-editor apps/web/package.json apps/web/next.config.ts pnpm-lock.yaml
git commit -m "chore: add block content editor package shell"
```

Expected: one commit with the package shell and dependency wiring.

## Task 3: Expand The Courses Feature Package Boundary

**Files:**
- Modify: `packages/features/courses/package.json`
- Modify: `packages/features/courses/src/index.ts`
- Modify: `apps/web/package.json`
- Modify: `apps/web/next.config.ts`
- Modify: `apps/learning/package.json`
- Modify: `apps/learning/next.config.ts` or `apps/learning/next.config.mjs`

- [ ] **Step 1: Add package scripts to courses**

Modify `packages/features/courses/package.json` to include:

```json
"scripts": {
  "typecheck": "tsc --noEmit --pretty false"
}
```

Do not remove the existing `exports`.

- [ ] **Step 2: Ensure courses exports support nested folders**

Modify `packages/features/courses/package.json` exports so it contains:

```json
"exports": {
  ".": "./src/index.ts",
  "./actions": "./src/actions.ts",
  "./components/*": "./src/components/*.tsx",
  "./components/common/*": "./src/components/common/*.tsx",
  "./components/course/*": "./src/components/course/*.tsx",
  "./components/course-editor/*": "./src/components/course-editor/*.tsx",
  "./components/editor/*": "./src/components/editor/*.tsx",
  "./components/forms/*": "./src/components/forms/*.tsx",
  "./components/learning/*": "./src/components/learning/*.tsx",
  "./components/sections/*": "./src/components/sections/*.tsx",
  "./components/tracks/*": "./src/components/tracks/*.tsx",
  "./types": "./src/types.ts"
}
```

If nested files include deeper folders, add exact export patterns for those folders during Task 5 when the failing import is known.

- [ ] **Step 3: Add courses dependency to the learning app**

Modify `apps/learning/package.json` dependencies to include:

```json
"@game-guild/courses": "workspace:*"
```

Do not remove existing dependencies.

- [ ] **Step 4: Transpile courses in both apps**

In `apps/web/next.config.ts`, ensure `transpilePackages` includes:

```ts
'@game-guild/courses'
```

In the learning app Next config file, ensure `transpilePackages` includes:

```ts
'@game-guild/ui',
'@game-guild/courses'
```

Keep any existing transpiled packages.

- [ ] **Step 5: Verify the package boundary before moving components**

Run:

```powershell
pnpm install --lockfile-only
pnpm --filter @game-guild/courses typecheck
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
pnpm --filter @game-guild/learning build
```

Expected: all commands pass.

- [ ] **Step 6: Commit the courses boundary**

Run:

```powershell
git add packages/features/courses apps/web/package.json apps/web/next.config.ts apps/learning/package.json apps/learning/next.config.* pnpm-lock.yaml
git commit -m "chore: prepare courses feature package boundary"
```

Expected: one commit with only package boundary and app dependency changes.

## Task 4: Move Course Components Into `@game-guild/courses`

**Files:**
- Move: `apps/web/src/components/courses/common` to `packages/features/courses/src/components/common`
- Move: `apps/web/src/components/courses/course` to `packages/features/courses/src/components/course`
- Move: `apps/web/src/components/courses/course-editor` to `packages/features/courses/src/components/course-editor`
- Move: `apps/web/src/components/courses/editor` to `packages/features/courses/src/components/editor`
- Move: `apps/web/src/components/courses/forms` to `packages/features/courses/src/components/forms`
- Move: `apps/web/src/components/courses/learning` to `packages/features/courses/src/components/learning`
- Move: `apps/web/src/components/courses/sections` to `packages/features/courses/src/components/sections`
- Move: `apps/web/src/components/courses/tracks` to `packages/features/courses/src/components/tracks`
- Move: app-agnostic `.tsx` files directly under `apps/web/src/components/courses` to `packages/features/courses/src/components`
- Keep in app: files with `"use server"`, route-only loaders, or direct `next/headers` or `next/cookies` usage
- Modify: `packages/features/courses/src/index.ts`
- Modify: all web and learning imports that point to moved files

- [ ] **Step 1: Identify server-only course files**

Run:

```powershell
rg -n "\"use server\"|next/headers|next/cookies|cookies\\(|headers\\(" apps/web/src/components/courses
```

Expected: any matching files stay in `apps/web`. All non-matching reusable component files can move.

- [ ] **Step 2: Move reusable course folders**

Run:

```powershell
New-Item -ItemType Directory -Force packages/features/courses/src/components | Out-Null
git mv apps/web/src/components/courses/common packages/features/courses/src/components/common
git mv apps/web/src/components/courses/course packages/features/courses/src/components/course
git mv apps/web/src/components/courses/course-editor packages/features/courses/src/components/course-editor
git mv apps/web/src/components/courses/editor packages/features/courses/src/components/editor
git mv apps/web/src/components/courses/forms packages/features/courses/src/components/forms
git mv apps/web/src/components/courses/learning packages/features/courses/src/components/learning
git mv apps/web/src/components/courses/sections packages/features/courses/src/components/sections
git mv apps/web/src/components/courses/tracks packages/features/courses/src/components/tracks
```

Expected: Git records file moves instead of deletes and adds.

- [ ] **Step 3: Move reusable root course components**

For each root file under `apps/web/src/components/courses` that does not match Step 1, move it:

```powershell
git mv apps/web/src/components/courses/browse-owned-courses.tsx packages/features/courses/src/components/browse-owned-courses.tsx
git mv apps/web/src/components/courses/course-card.tsx packages/features/courses/src/components/course-card.tsx
git mv apps/web/src/components/courses/course-content-layout.tsx packages/features/courses/src/components/course-content-layout.tsx
git mv apps/web/src/components/courses/course-content-sidebar.tsx packages/features/courses/src/components/course-content-sidebar.tsx
git mv apps/web/src/components/courses/course-content.tsx packages/features/courses/src/components/course-content.tsx
git mv apps/web/src/components/courses/course-context.tsx packages/features/courses/src/components/course-context.tsx
git mv apps/web/src/components/courses/course-create-drawer.tsx packages/features/courses/src/components/course-create-drawer.tsx
git mv apps/web/src/components/courses/course-error-boundary.tsx packages/features/courses/src/components/course-error-boundary.tsx
git mv apps/web/src/components/courses/course-filter.tsx packages/features/courses/src/components/course-filter.tsx
git mv apps/web/src/components/courses/course-grid-enhanced.tsx packages/features/courses/src/components/course-grid-enhanced.tsx
git mv apps/web/src/components/courses/course-grid.tsx packages/features/courses/src/components/course-grid.tsx
git mv apps/web/src/components/courses/course-highlight-carousel.tsx packages/features/courses/src/components/course-highlight-carousel.tsx
git mv apps/web/src/components/courses/course-list-wrapper.tsx packages/features/courses/src/components/course-list-wrapper.tsx
git mv apps/web/src/components/courses/course-list.tsx packages/features/courses/src/components/course-list.tsx
git mv apps/web/src/components/courses/course-location-selector.tsx packages/features/courses/src/components/course-location-selector.tsx
git mv apps/web/src/components/courses/course-page-error.tsx packages/features/courses/src/components/course-page-error.tsx
git mv apps/web/src/components/courses/course-states.tsx packages/features/courses/src/components/course-states.tsx
git mv apps/web/src/components/courses/course-sub-nav.tsx packages/features/courses/src/components/course-sub-nav.tsx
git mv apps/web/src/components/courses/courses-overview-content.tsx packages/features/courses/src/components/courses-overview-content.tsx
git mv apps/web/src/components/courses/location-based-content.tsx packages/features/courses/src/components/location-based-content.tsx
git mv apps/web/src/components/courses/public-course-catalog.tsx packages/features/courses/src/components/public-course-catalog.tsx
git mv apps/web/src/components/courses/sidebar-context.tsx packages/features/courses/src/components/sidebar-context.tsx
git mv apps/web/src/components/courses/sidebar-toggle.tsx packages/features/courses/src/components/sidebar-toggle.tsx
```

If a listed file no longer exists because it was already moved or renamed, record the actual path in the execution notes and continue with the remaining files.

- [ ] **Step 4: Move course component tests with their files**

Run:

```powershell
Get-ChildItem -Recurse packages/features/courses/src/components -Filter *.test.tsx | Select-Object -ExpandProperty FullName
Get-ChildItem -Recurse packages/features/courses/src/components -Filter *.test.ts | Select-Object -ExpandProperty FullName
```

Expected: tests that lived beside moved components now live in the package with those components.

- [ ] **Step 5: Rewrite imports from app alias to package alias**

Run this search:

```powershell
rg -n "@/components/courses|components/courses" apps/web/src apps/learning/src packages/features/courses/src
```

For each import that points at a moved component, replace it with `@game-guild/courses/...`. Examples:

```ts
import { CourseLandingPage } from '@game-guild/courses/components/course/course-landing-page';
import { CourseContentViewer } from '@game-guild/courses/components/learning/course-content-viewer';
import { CourseCard } from '@game-guild/courses/components/course-card';
```

For package-internal imports, prefer relative imports when both files live under `packages/features/courses/src/components`.

- [ ] **Step 6: Update the courses package entry point**

Modify `packages/features/courses/src/index.ts` to export commonly shared components:

```ts
export { CourseCatalog } from './components/course-catalog';
export { PublicCourseCatalog } from './components/public-course-catalog';
export { CourseCard } from './components/course-card';
export { CourseGrid } from './components/course-grid';
export { CourseList } from './components/course-list';
export { CourseHighlightCarousel } from './components/course-highlight-carousel';
export type { CourseCatalogProps, CourseSummary } from './types';
```

If any exported component has a different named export, use the component file's actual export name.

- [ ] **Step 7: Verify no app-local course imports remain**

Run:

```powershell
rg -n "@/components/courses|components/courses" apps/web/src apps/learning/src packages/features/courses/src
```

Expected: no output for moved component imports. Server-only files left in `apps/web/src/components/courses` may still use relative imports to app-local server helpers.

- [ ] **Step 8: Verify courses package and apps**

Run:

```powershell
pnpm --filter @game-guild/courses typecheck
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
pnpm --filter @game-guild/web test -- packages/features/courses/src
pnpm --filter @game-guild/web build
pnpm --filter @game-guild/learning build
```

Expected: all commands pass. If Vitest cannot discover package tests through the web filter, run the exact moved test files through the configured test command that currently owns those tests.

- [ ] **Step 9: Commit the courses migration**

Run:

```powershell
git add packages/features/courses apps/web apps/learning pnpm-lock.yaml
git commit -m "refactor: move course components into feature package"
```

Expected: one commit with course component moves and import rewrites.

## Task 5: Move The Block Content Editor Into `@game-guild/block-content-editor`

**Files:**
- Move: `apps/web/src/components/block-content-editor/*` to `packages/features/block-content-editor/src/*`
- Modify: `packages/features/block-content-editor/src/index.ts`
- Modify: `apps/web/src/app/[locale]/(block-content-editor)/**`
- Modify: `apps/web/src/app/api/static-viewer/**`
- Modify: package-internal imports in `packages/features/block-content-editor/src/**`

- [ ] **Step 1: Move the editor tree exactly**

Run:

```powershell
New-Item -ItemType Directory -Force packages/features/block-content-editor/src | Out-Null
git mv apps/web/src/components/block-content-editor/docs packages/features/block-content-editor/src/docs
git mv apps/web/src/components/block-content-editor/embed packages/features/block-content-editor/src/embed
git mv apps/web/src/components/block-content-editor/engines packages/features/block-content-editor/src/engines
git mv apps/web/src/components/block-content-editor/extras packages/features/block-content-editor/src/extras
git mv apps/web/src/components/block-content-editor/hooks packages/features/block-content-editor/src/hooks
git mv apps/web/src/components/block-content-editor/lexical-surface packages/features/block-content-editor/src/lexical-surface
git mv apps/web/src/components/block-content-editor/lib packages/features/block-content-editor/src/lib
git mv apps/web/src/components/block-content-editor/nodes packages/features/block-content-editor/src/nodes
git mv apps/web/src/components/block-content-editor/plugins packages/features/block-content-editor/src/plugins
git mv apps/web/src/components/block-content-editor/services packages/features/block-content-editor/src/services
git mv apps/web/src/components/block-content-editor/utils packages/features/block-content-editor/src/utils
git mv apps/web/src/components/block-content-editor/lazy-client-components.tsx packages/features/block-content-editor/src/lazy-client-components.tsx
git mv apps/web/src/components/block-content-editor/theme-provider.tsx packages/features/block-content-editor/src/theme-provider.tsx
git mv apps/web/src/components/block-content-editor/top-menu.tsx packages/features/block-content-editor/src/top-menu.tsx
git mv apps/web/src/components/block-content-editor/youtube-audio-style.tsx packages/features/block-content-editor/src/youtube-audio-style.tsx
git mv apps/web/src/components/block-content-editor/PROJECT_NAVIGATION.md packages/features/block-content-editor/src/PROJECT_NAVIGATION.md
```

Expected: the editor code is moved without behavior changes.

- [ ] **Step 2: Rewrite package-internal editor imports**

Run:

```powershell
rg -n "@/components/block-content-editor|components/block-content-editor" packages/features/block-content-editor/src
```

For each internal import, replace the app alias with the package alias. Example:

```ts
import { TopMenu } from '@game-guild/block-content-editor/top-menu';
import { sanitizeHtml } from '@game-guild/block-content-editor/lib/sanitize-html';
import { YouTubeAudioStyle } from '@game-guild/block-content-editor/youtube-audio-style';
```

When two files are in the same folder or nearby folders, relative imports are also acceptable and reduce exported surface area:

```ts
import { sanitizeHtml } from '../lib/sanitize-html';
```

- [ ] **Step 3: Rewrite web route imports**

Run:

```powershell
rg -n "@/components/block-content-editor|components/block-content-editor" apps/web/src/app apps/web/src/lib
```

Replace route imports with package imports. Example:

```ts
import { LazyBlockContentEditor } from '@game-guild/block-content-editor/lazy-client-components';
import { BlockContentEditorThemeProvider } from '@game-guild/block-content-editor/theme-provider';
```

Keep the route files under `apps/web/src/app/[locale]/(block-content-editor)`; only imports change.

- [ ] **Step 4: Export route-facing editor components**

Modify `packages/features/block-content-editor/src/index.ts` to contain:

```ts
export { default as BlockContentEditorThemeProvider } from './theme-provider';
export { default as TopMenu } from './top-menu';
export { default as YouTubeAudioStyle } from './youtube-audio-style';
```

If the moved files use named exports instead of default exports, use the actual export form from those files.

- [ ] **Step 5: Verify no app-local editor imports remain**

Run:

```powershell
rg -n "@/components/block-content-editor|components/block-content-editor" apps/web/src packages/features/block-content-editor/src
```

Expected: no output.

- [ ] **Step 6: Verify block editor package and web app**

Run:

```powershell
pnpm --filter @game-guild/block-content-editor typecheck
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
pnpm --filter @game-guild/web test -- src/components/block-content-editor.migration.test.ts src/lib/__tests__/web-runtime-hardening.test.ts
pnpm --filter @game-guild/web test:browser:block-editor
pnpm --filter @game-guild/web build
```

Expected: all commands pass.

- [ ] **Step 7: Commit the block editor migration**

Run:

```powershell
git add packages/features/block-content-editor apps/web pnpm-lock.yaml
git commit -m "refactor: move block editor into feature package"
```

Expected: one commit with the editor move and import rewrites.

## Task 6: Extract Shared Site Components Only When Reused

**Files:**
- Create only if both apps need these components: `packages/features/site/package.json`
- Create only if both apps need these components: `packages/features/site/tsconfig.json`
- Create only if both apps need these components: `packages/features/site/src/index.ts`
- Move only app-agnostic files from: `apps/web/src/components/site`
- Modify: `apps/web/package.json`
- Modify: `apps/learning/package.json`
- Modify: app imports that point to moved site components

- [ ] **Step 1: Check whether site components are reused by more than one app**

Run:

```powershell
rg -n "@/components/site|components/site" apps/web/src apps/learning/src
```

Expected: if only `apps/web` imports these files, do not create `@game-guild/site`. Keep them in `apps/web`.

- [ ] **Step 2: Create `@game-guild/site` only if Step 1 shows multi-app reuse**

If both `apps/web` and `apps/learning` import public site components, create `packages/features/site/package.json` with:

```json
{
  "name": "@game-guild/site",
  "version": "0.1.0",
  "type": "module",
  "description": "Reusable public site components for GameGuild apps",
  "license": "UNLICENSED",
  "private": true,
  "exports": {
    ".": "./src/index.ts",
    "./components/*": "./src/components/*.tsx"
  },
  "scripts": {
    "typecheck": "tsc --noEmit --pretty false"
  },
  "dependencies": {
    "@game-guild/ui": "workspace:*",
    "lucide-react": "^0.525.0",
    "next": "16.2.9",
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  },
  "devDependencies": {
    "@game-guild/eslint-config": "workspace:*",
    "@game-guild/prettier-config": "workspace:*",
    "@game-guild/typescript-config": "workspace:*",
    "@types/node": "^20.0.0",
    "@types/react": "^19.0.0",
    "@types/react-dom": "^19.0.0",
    "eslint": "^9.0.0",
    "typescript": "^5.5.0"
  }
}
```

- [ ] **Step 3: Skip extraction if Step 1 shows web-only ownership**

If only `apps/web` imports `apps/web/src/components/site`, record this in the execution notes:

```text
Site components remain app-owned because only apps/web consumes them.
```

No commit is required for this task when extraction is skipped.

- [ ] **Step 4: Verify site boundary decision**

Run:

```powershell
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
pnpm --filter @game-guild/learning build
```

Expected: both commands pass.

## Task 6b: Extract Shared Auth Components Only After Decoupling

**Files:**
- Create only after callback-based decoupling: `packages/features/auth-components/package.json`
- Create only after callback-based decoupling: `packages/features/auth-components/tsconfig.json`
- Create only after callback-based decoupling: `packages/features/auth-components/src/index.ts`
- Move only app-agnostic form components from: `apps/web/src/components/*form*.tsx`
- Move only app-agnostic form components from: `apps/learning/src/components/*form*.tsx`
- Modify: `apps/web/src/app/[locale]/(auth)/**`
- Modify: `apps/learning/src/app/**`

- [ ] **Step 1: Find auth component duplication**

Run:

```powershell
rg -n "sign[-]?in|sign[-]?up|forgot|otp|credentials|next-auth|useActionState|useFormStatus" apps/web/src/components apps/learning/src/components apps/web/src/app apps/learning/src/app
```

Expected: duplicated form UI is identified, and components with app-specific auth behavior are not moved directly.

- [ ] **Step 2: Refactor form UI before package move**

For each duplicated form, split it into:

```text
App route/page: owns session provider, auth action, redirect, localized routing.
Package form component: owns fields, labels, validation display, loading display, and submit event callback.
```

Expected: package components receive `onSubmit`, `initialValues`, `status`, `links`, and copy strings through props.

- [ ] **Step 3: Create package only after Step 2 passes**

Create `packages/features/auth-components/package.json` with package name `@game-guild/auth-components`, a `typecheck` script, `@game-guild/ui`, `react`, `react-dom`, `next`, `lucide-react`, and `react-hook-form` dependencies.

- [ ] **Step 4: Verify auth flows**

Run:

```powershell
pnpm --filter @game-guild/auth-components typecheck
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
pnpm --filter @game-guild/learning typecheck
pnpm --filter @game-guild/web test:e2e
```

Expected: auth forms compile in both apps, and app routes still own real sign-in/sign-up behavior.

## Task 6c: Extract Content Rendering Components

**Files:**
- Create: `packages/features/content-rendering/package.json`
- Create: `packages/features/content-rendering/tsconfig.json`
- Create: `packages/features/content-rendering/src/index.ts`
- Move: `apps/web/src/components/markdown-renderer`
- Move equivalent learning markdown renderer if present: `apps/learning/src/components/markdown-renderer.tsx`
- Modify: course and learning imports that render markdown/content

- [ ] **Step 1: Compare markdown renderers**

Run:

```powershell
rg --files apps/web/src/components/markdown-renderer apps/learning/src/components | rg "markdown|content"
```

Expected: duplicate or overlapping markdown/content renderers are listed.

- [ ] **Step 2: Create `@game-guild/content-rendering`**

Create package with exports:

```json
"exports": {
  ".": "./src/index.ts",
  "./components/*": "./src/components/*.tsx"
}
```

Expected: shared content renderers have a domain package distinct from the block editor.

- [ ] **Step 3: Move app-agnostic renderers**

Move markdown/content renderer files that do not import app routing/auth/server code.

Expected: course learning components can import renderers from `@game-guild/content-rendering`.

- [ ] **Step 4: Verify renderer package**

Run:

```powershell
pnpm --filter @game-guild/content-rendering typecheck
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
pnpm --filter @game-guild/learning build
```

Expected: both apps compile with the shared renderer package.

## Task 6d: Prepare Platform And Public Shell Packages Without Moving App-Coupled Code

**Files:**
- Keep: `apps/web/src/components/layout`
- Keep: `apps/web/src/components/site`
- Create documentation only: `docs/component-package-inventory.md`

- [ ] **Step 1: Confirm shell app coupling**

Run:

```powershell
rg -n "@/auth|@/i18n|@/lib/dashboard|next/link|next/navigation|next-intl" apps/web/src/components/layout apps/web/src/components/site
```

Expected: output confirms these shells currently depend on app-owned routing/auth/navigation.

- [ ] **Step 2: Do not move these shells yet**

Record in execution notes:

```text
Dashboard and public website shell remain app-owned until navigation, auth/session, user menu, notification, and localized route dependencies are injected as props.
```

Expected: architecture remains clean rather than moving app-coupled shells into packages prematurely.

## Task 6e: Recover Product Components From Main/Temp Into Context Packages

**Files:**
- Read: `.tmp/gameguild-main/apps/web/src/components`
- Read: `.tmp/gameguild-block-content-editor/apps/web/src/components`
- Create later as needed: `packages/features/projects`
- Create later as needed: `packages/features/programs`
- Create later as needed: `packages/features/testing-lab`
- Create later as needed: `packages/features/launch-pad`
- Create later as needed: `packages/features/community-feed`
- Create later as needed: `packages/features/notifications`

- [ ] **Step 1: Map old-main component contexts**

Run:

```powershell
if (Test-Path .tmp/gameguild-main/apps/web/src/components) {
  Get-ChildItem -Directory .tmp/gameguild-main/apps/web/src/components | Select-Object -ExpandProperty Name
}
if (Test-Path .tmp/gameguild-block-content-editor/apps/web/src/components) {
  Get-ChildItem -Directory .tmp/gameguild-block-content-editor/apps/web/src/components | Select-Object -ExpandProperty Name
}
```

Expected: old-main product component contexts are listed before any migration.

- [ ] **Step 2: Migrate one old-main product context at a time**

For each recovered context, create a feature package only when:

```text
1. the component has a real route/app consumer in this branch,
2. app-specific auth/routing/server code can be injected through props,
3. tests can be moved or written beside the package,
4. app routes remain in apps/web or apps/learning.
```

Expected: old implementation code is brought forward surgically, not copied wholesale.

## Task 7: Add Import Guardrails

**Files:**
- Create: `tools/check-feature-component-boundaries.mjs`
- Modify: `package.json`

- [ ] **Step 1: Add a boundary check script**

Create `tools/check-feature-component-boundaries.mjs` with:

```js
import { execFileSync } from 'node:child_process';

const forbiddenPatterns = [
  {
    label: 'app-local course component imports',
    pattern: '@/components/courses|components/courses',
  },
  {
    label: 'app-local block editor imports',
    pattern: '@/components/block-content-editor|components/block-content-editor',
  },
];

let failed = false;

for (const rule of forbiddenPatterns) {
  let output = '';
  try {
    output = execFileSync(
      'rg',
      ['-n', rule.pattern, 'apps/web/src', 'apps/learning/src', 'packages/features'],
      { encoding: 'utf8' },
    );
  } catch (error) {
    if (error.status === 1) {
      continue;
    }
    throw error;
  }

  const lines = output
    .split('\n')
    .filter(Boolean)
    .filter((line) => !line.includes('docs/superpowers/plans/'));

  if (lines.length > 0) {
    failed = true;
    console.error(`Forbidden ${rule.label}:`);
    for (const line of lines) {
      console.error(line);
    }
  }
}

if (failed) {
  process.exit(1);
}

console.log('Feature component boundaries are clean.');
```

- [ ] **Step 2: Add a root package script**

Modify root `package.json` scripts to include:

```json
"check:component-boundaries": "node tools/check-feature-component-boundaries.mjs"
```

Keep existing scripts unchanged.

- [ ] **Step 3: Verify the guardrail**

Run:

```powershell
pnpm check:component-boundaries
```

Expected:

```text
Feature component boundaries are clean.
```

- [ ] **Step 4: Commit the guardrail**

Run:

```powershell
git add tools/check-feature-component-boundaries.mjs package.json
git commit -m "test: guard feature component package boundaries"
```

Expected: one commit with the boundary script.

## Task 8: Full Verification And Final Cleanup

**Files:**
- Modify only files required by failures from previous tasks
- Read: `git status --short`

- [ ] **Step 1: Run workspace verification**

Run:

```powershell
pnpm check:component-boundaries
pnpm --filter @game-guild/courses typecheck
pnpm --filter @game-guild/block-content-editor typecheck
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
pnpm --filter @game-guild/web test -- src/components/block-content-editor.migration.test.ts src/lib/__tests__/web-runtime-hardening.test.ts
pnpm --filter @game-guild/web test:browser:block-editor
pnpm --filter @game-guild/web build
pnpm --filter @game-guild/learning build
```

Expected: all commands pass.

- [ ] **Step 2: Confirm route files still live in apps**

Run:

```powershell
Test-Path "apps/web/src/app/[locale]/(block-content-editor)"
Test-Path "apps/web/src/app/[locale]/(dashboard)"
Test-Path "apps/learning/src/app"
```

Expected: all three commands print `True`.

- [ ] **Step 3: Confirm feature packages do not import app code**

Run:

```powershell
rg -n "apps/web|apps/learning|@/app|next/headers|next/cookies" packages/features/courses/src packages/features/block-content-editor/src
```

Expected: no output. If output appears from a package file, move that file back to the owning app or inject the value as a prop from the app.

- [ ] **Step 4: Confirm worktree state**

Run:

```powershell
git status --short
```

Expected: only intended migration changes are present before the final commit.

- [ ] **Step 5: Commit final cleanup if needed**

Run only if Step 1 through Step 4 required cleanup changes:

```powershell
git add packages/features apps/web apps/learning tools package.json pnpm-lock.yaml
git commit -m "chore: finalize feature component package migration"
```

Expected: the worktree has no uncommitted migration changes after this commit.

## Self-Review

- Spec coverage: the plan moves reusable components outside `web` and `learning`, keeps them out of `ui`, and creates one package per context.
- App ownership preserved: routes, server actions, app data loading, and deployment config stay in apps.
- Package ownership preserved: courses and block editor get their own feature packages under the existing `packages/features/*` workspace.
- Test coverage: each migration has typecheck, unit/browser tests, app builds, and a boundary script.
- React Compiler: the plan keeps compiler support enabled and requires fixing moved package code if compiler issues appear.
