# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS pruner
ENV PNPM_HOME=/pnpm
ENV PATH=$PNPM_HOME:$PATH
RUN corepack enable \
    && corepack prepare pnpm@10.0.0 --activate \
    && pnpm add --global turbo@2.10.11
WORKDIR /repo
COPY . .
RUN turbo prune @game-guild/learning --docker

FROM node:22-alpine AS deps
ENV PNPM_HOME=/pnpm
ENV PATH=$PNPM_HOME:$PATH
RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo
COPY --from=pruner /repo/out/json/ ./
COPY --from=pruner /repo/out/pnpm-lock.yaml ./pnpm-lock.yaml
RUN --mount=type=cache,id=pnpm-learning,target=/pnpm/store \
    pnpm install --frozen-lockfile --prefer-offline --store-dir=/pnpm/store

FROM deps AS builder
ARG GIT_HASH
ARG AUTH_SECRET
ARG API_INTERNAL_URL
ARG API_PUBLIC_URL
ARG LEARNING_PUBLIC_URL
ARG WEB_PUBLIC_URL
ARG AUTH_COOKIE_DOMAIN
ARG SOURCE_SHA=unknown
ARG SOURCE_TREE=unknown
ARG RELEASE_SHA=unknown
ARG VERSION=unknown
ARG BUILD_TIMESTAMP=unknown

ENV GIT_HASH=${GIT_HASH}
ENV NEXT_TELEMETRY_DISABLED=1
ENV NEXT_PRIVATE_BUILD_WORKER_COUNT=1
ENV NODE_OPTIONS=--max-old-space-size=2048
ENV NEXTAUTH_URL=${LEARNING_PUBLIC_URL}
ENV AUTH_TRUST_HOST=true
ENV API_URL=${API_INTERNAL_URL:-http://gameguild-api-internal:3000}
ENV NEXT_PUBLIC_API_URL=${API_PUBLIC_URL}
ENV NEXT_PUBLIC_WEB_URL=${WEB_PUBLIC_URL}
ENV AUTH_COOKIE_DOMAIN=${AUTH_COOKIE_DOMAIN}
ENV GAMEGUILD_VERSION=${VERSION}
ENV RELEASE_SHA=${RELEASE_SHA}
ENV SOURCE_TREE=${SOURCE_TREE}
ENV BUILT_AT=${BUILD_TIMESTAMP}

COPY --from=pruner /repo/out/full/ ./
RUN --mount=type=secret,id=auth_secret,required=false \
    mounted_secret="$(cat /run/secrets/auth_secret 2>/dev/null || true)" \
    && export AUTH_SECRET="${mounted_secret:-$AUTH_SECRET}" \
    && if [ -z "$AUTH_SECRET" ]; then echo "AUTH_SECRET build secret is required" >&2; exit 1; fi \
    && pnpm --filter @game-guild/client build \
    && pnpm --filter @game-guild/learning build

FROM node:22-alpine AS runner
ARG SOURCE_SHA=unknown
ARG SOURCE_TREE=unknown
ARG RELEASE_SHA=unknown
ARG VERSION=unknown
ARG BUILD_TIMESTAMP=unknown

LABEL org.opencontainers.image.source="https://github.com/gameguild-gg/gameguild" \
      org.opencontainers.image.revision="${SOURCE_SHA}" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.created="${BUILD_TIMESTAMP}" \
      io.gameguild.source-tree="${SOURCE_TREE}"

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
ENV GAMEGUILD_VERSION=${VERSION}
ENV RELEASE_SHA=${RELEASE_SHA}
ENV SOURCE_TREE=${SOURCE_TREE}
ENV BUILT_AT=${BUILD_TIMESTAMP}
ENV IMAGE_DIGEST=Unknown
ENV DEPLOYED_AT=Unknown

WORKDIR /app
RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /repo/apps/learning/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/learning/.next/static ./apps/learning/.next/static

USER nextjs
EXPOSE 3000
CMD ["node", "apps/learning/server.js"]
