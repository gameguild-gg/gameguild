# Precompiled Learning Development Environment

This guide describes the specialized local environment started by
`pnpm run dev:learning`. It is intended for sustained functional testing of the
Learning dashboard without compiling each page on its first visit.

This mode complements `pnpm run dev`; it does not replace the standard
hot-reload development workflow.

## When to use it

Use `dev:learning` when you need to navigate repeatedly through the instructor
Learning experience and prefer a longer startup followed by stable navigation.
Use the standard `pnpm run dev` command while actively editing code and relying
on HMR.

## What it starts

The command performs these steps in order:

1. starts PostgreSQL, Redis, Garage, and `garage-init` with Docker Compose;
2. starts the API locally with `dotnet run` on port `8080`;
3. regenerates and builds `@game-guild/client` from the running API;
4. creates a selective Next.js production build for the Learning surface;
5. starts the precompiled web application with `next start` on port `3000`.

The Next.js build is stored in `apps/web/.next-learning`. It is isolated from
the standard `.next` directory, so switching between `dev` and `dev:learning`
does not make the two modes share their Next.js artifacts.

## Compiled route scope

The selective build includes:

- the public root page and authentication pages;
- `/dashboard` and its redirects into the current workspace;
- the `/workspace` hub;
- every page under `/workspace/learning`;
- Learning authoring pages, including course-content editing;
- route handlers required by authentication, health checks, assets, courses,
  and Learning authoring.

Other workspace areas are intentionally outside this specialized build. Use
`pnpm run dev` or the regular full production build when testing across product
areas.

## Starting the environment

Make sure another local API or web process is not already using ports `8080`
or `3000`, then run from the repository root:

```bash
pnpm run dev:learning
```

Wait for the following message before opening the browser:

```text
[dev:learning] ready at http://localhost:3000/dashboard (precompiled, no HMR)
```

Open:

```text
http://localhost:3000/dashboard
```

The dashboard redirects into the workspace, where the precompiled Learning
routes are available.

## Runtime and memory behavior

The build runs before the web server starts. The Webpack compiler does not stay
resident while the environment is being used, and the Node.js processes receive
a 4 GiB heap limit. This avoids retaining a large collection of development
compiler entries in memory.

The mode does not provide HMR. After changing source code, stop the process and
run `pnpm run dev:learning` again. Existing build caches may reduce subsequent
build times.

## Stopping and cleaning

Press `Ctrl+C` once and wait for `[dev:learning] shutting down...`. The
orchestrator stops the API, web server, and any active build child processes.
Infrastructure containers remain available for the next startup.

To stop the containers as well:

```bash
pnpm run dev:stop
```

To stop the environment and remove generated artifacts, including
`apps/web/.next-learning`:

```bash
pnpm run dev:repair
```

For general process, port, Docker, and cache recovery guidance, see
[`local-development-cleanup.md`](./local-development-cleanup.md).

## Troubleshooting

### Port already in use

If startup reports that port `3000` or `8080` is occupied, stop the previous
environment before retrying:

```bash
pnpm run dev:stop
pnpm run dev:learning
```

Use `pnpm run dev:repair` instead when a previous run also left stale generated
artifacts.

### Build succeeds but a route is unavailable

Confirm that the route belongs to `/dashboard`, `/workspace`, or
`/workspace/learning`. Other workspace product areas are not part of this
selective build.

If a Learning route is missing, inspect the route summary printed by the Next.js
build. Pages whose URL belongs to Learning may live in a different App Router
route group, such as `(authoring)`, and must be included by the selective build
patterns in `apps/web/scripts/build-learning.mjs`.

### API or generated-client failure

The web build starts only after the API health check and generated-client build
succeed. Inspect the same terminal for the first failing step. The orchestrator
stops its child processes when any required step fails.
