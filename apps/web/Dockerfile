# syntax=docker/dockerfile:1.6

# ─── emception-wasm: build the WASM toolchain + CDN bundles ─────────────────
# Heavy stage: ~40m build, ~12GB intermediate. Cached by BuildKit as long as
# tools/emception/** source is unchanged. Output is only the public/cdn tree
# (~180MB), copied into deps via COPY --from=emception-wasm below.
#
# Uses bookworm-slim (glibc) instead of alpine: emsdk ships prebuilt clang/llvm
# linked against glibc; on alpine (musl) the binaries fail to execute.
FROM node:20-bookworm-slim AS emception-wasm

# Native deps for the WASM build (mirrors .github/workflows/emception.yml L113-117)
RUN apt-get update && apt-get install -y --no-install-recommends \
    brotli \
    build-essential \
    cmake \
    ninja-build \
    pkg-config \
    python3 \
    git \
    curl \
    ca-certificates \
    xz-utils \
    bzip2 \
    # CPython build deps (CPython 3.13 needs zlib to bootstrap pip via wheels;
    # other libs cover ssl/ffi/bz2/sqlite/lzma/readline/ncurses modules that
    # the bundled cpython compiles). ubuntu-latest ships these pre-installed;
    # bookworm-slim does not.
    zlib1g-dev \
    libssl-dev \
    libffi-dev \
    libbz2-dev \
    libsqlite3-dev \
    liblzma-dev \
    libreadline-dev \
    libncursesw5-dev \
    uuid-dev \
    && rm -rf /var/lib/apt/lists/*

RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo

# Copy emception toolchain sources. BuildKit caches each COPY layer; if
# tools/emception/** is unchanged, the subsequent build:all layer also hits
# cache (no rebuild).
COPY pnpm-workspace.yaml .npmrc ./
COPY tools/emception/package.json ./tools/emception/
COPY tools/emception/tsconfig.base.json ./tools/emception/
COPY tools/emception/tsconfig.json ./tools/emception/
COPY tools/emception/tsconfig.lib.json ./tools/emception/
COPY tools/emception/packages ./tools/emception/packages
COPY tools/emception/scripts ./tools/emception/scripts
COPY tools/emception/src ./tools/emception/src
COPY tools/emception/userland ./tools/emception/userland
COPY tools/emception/public/coi-serviceworker.js ./tools/emception/public/coi-serviceworker.js

# Strip root package.json to bare minimum — full root has workspace:* devDeps
# (@game-guild/eslint-config etc.) that aren't copied into this stage. pnpm
# install is for emception workspace only.
RUN echo '{"name":"game-guild","version":"0.1.0","private":true,"packageManager":"pnpm@10.0.0"}' > package.json

# Install emception workspace deps (npm-run-all, tsx, typescript)
RUN pnpm install --no-frozen-lockfile --prefer-offline

# Build the WASM toolchain + CDN bundles. This is the 40m step.
# Outputs to /repo/tools/emception/public/cdn/ (manifest.json + .tar.br bundles).
RUN pnpm --dir tools/emception run build:all

# ─── deps: workspace install + emception TS packages ────────────────────────
FROM node:20-alpine AS deps
RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo

COPY pnpm-workspace.yaml package.json .npmrc* ./
COPY apps/web/package.json ./apps/web/
COPY packages ./packages
COPY tools/emception/package.json ./tools/emception/
COPY tools/emception/tsconfig.base.json ./tools/emception/
COPY tools/emception/packages ./tools/emception/packages
COPY tools/emception/public/coi-serviceworker.js ./tools/emception/public/coi-serviceworker.js
# Pull the built WASM CDN from the heavy stage (single COPY; only ~180MB).
COPY --from=emception-wasm /repo/tools/emception/public/cdn ./tools/emception/public/cdn

RUN pnpm install --no-frozen-lockfile --prefer-offline

# Build emception workspace packages (they ship TS source; dist/ required by apps/web build)
RUN pnpm --dir tools/emception run build:packages

FROM deps AS builder
ARG GIT_HASH
ARG AUTH_SECRET
ARG API_INTERNAL_URL
ARG API_PUBLIC_URL
ARG WEB_PUBLIC_URL
ARG LEARNING_PUBLIC_URL
ARG AUTH_COOKIE_DOMAIN
ARG UNIFIED_LEARNING_ENABLED=false
ARG NEXT_PUBLIC_GOOGLE_CLIENT_ID

ENV GIT_HASH=${GIT_HASH}
ENV NEXT_TELEMETRY_DISABLED=1
ENV NEXT_PRIVATE_BUILD_WORKER_COUNT=1
ENV NODE_OPTIONS=--max-old-space-size=4096
ENV AUTH_SECRET=${AUTH_SECRET}
ENV NEXTAUTH_URL=${WEB_PUBLIC_URL}
ENV AUTH_TRUST_HOST=true
ENV API_URL=${API_INTERNAL_URL:-http://gameguild-api-internal:3000}
ENV NEXT_PUBLIC_API_URL=${API_PUBLIC_URL}
ENV NEXT_PUBLIC_APP_URL=${WEB_PUBLIC_URL}
ENV NEXT_PUBLIC_LEARNING_APP_URL=${LEARNING_PUBLIC_URL}
ENV NEXT_PUBLIC_GOOGLE_CLIENT_ID=${NEXT_PUBLIC_GOOGLE_CLIENT_ID}
ENV WEB_PUBLIC_URL=${WEB_PUBLIC_URL}
ENV LEARNING_PUBLIC_URL=${LEARNING_PUBLIC_URL}
ENV AUTH_COOKIE_DOMAIN=${AUTH_COOKIE_DOMAIN}
ENV UNIFIED_LEARNING_ENABLED=${UNIFIED_LEARNING_ENABLED}

COPY . .

RUN if [ -z "$AUTH_SECRET" ]; then echo "AUTH_SECRET build arg is required" >&2; exit 1; fi \
    && pnpm --filter @game-guild/client build \
    && (pnpm --filter @game-guild/web build & pid=$!; \
        while kill -0 "$pid" 2>/dev/null; do \
          echo "web production build still running..."; \
          sleep 30; \
        done; \
        wait "$pid")

FROM node:20-alpine AS runner

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

WORKDIR /app

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/static ./apps/web/.next/static
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/public ./apps/web/public

USER nextjs
EXPOSE 3000

CMD ["node", "apps/web/server.js"]
