# syntax=docker/dockerfile:1.6

FROM node:20-alpine AS deps
RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo

COPY pnpm-workspace.yaml package.json .npmrc* ./
COPY apps/web/package.json ./apps/web/
COPY packages ./packages
COPY tools/emception/package.json ./tools/emception/
COPY tools/emception/tsconfig.base.json ./tools/emception/
COPY tools/emception/scripts ./tools/emception/scripts
COPY tools/emception/packages ./tools/emception/packages
COPY tools/emception/public/coi-serviceworker.js ./tools/emception/public/coi-serviceworker.js

RUN pnpm install --no-frozen-lockfile --prefer-offline

# Build emception workspace packages (ship TS source; dist/ required by apps/web build)
RUN pnpm --dir tools/emception run build:packages

RUN TOOLCHAIN_VERSION="$(node -p 'require("./tools/emception/packages/toolchain/package.json").version')" && \
    mkdir -p /repo/tools/emception/artifacts/toolchain/release/cdn /tmp/emception-extract && \
    cd /tmp/emception-extract && \
    npm pack "emception@${TOOLCHAIN_VERSION}" && \
    tar xzf *.tgz && \
    cp -r /tmp/emception-extract/package/cdn/. /repo/tools/emception/artifacts/toolchain/release/cdn/ && \
    rm -rf /tmp/emception-extract

FROM deps AS builder
ARG GIT_HASH
ARG AUTH_SECRET
ARG API_INTERNAL_URL
ARG API_PUBLIC_URL
ARG WEB_PUBLIC_URL
ARG AUTH_COOKIE_DOMAIN
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
ENV NEXT_PUBLIC_GOOGLE_CLIENT_ID=${NEXT_PUBLIC_GOOGLE_CLIENT_ID}
ENV WEB_PUBLIC_URL=${WEB_PUBLIC_URL}
ENV AUTH_COOKIE_DOMAIN=${AUTH_COOKIE_DOMAIN}

COPY . .

RUN if [ -z "$AUTH_SECRET" ]; then echo "AUTH_SECRET build arg is required" >&2; exit 1; fi \
    && pnpm --filter @game-guild/client build \
    && pnpm --filter @game-guild/web build

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
COPY apps/web/static-cache-guard.cjs ./static-cache-guard.cjs

USER nextjs
EXPOSE 3000

CMD ["node", "static-cache-guard.cjs"]
