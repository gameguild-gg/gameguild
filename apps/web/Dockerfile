# syntax=docker/dockerfile:1.6

ARG NODE_VERSION=20-alpine
ARG APP_PORT=3000

FROM node:${NODE_VERSION} AS deps
RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo

COPY pnpm-workspace.yaml pnpm-lock.yaml package.json .npmrc* ./
COPY apps/web/package.json ./apps/web/
COPY packages ./packages

RUN pnpm install --frozen-lockfile --prefer-offline

FROM node:${NODE_VERSION} AS builder
ARG GIT_HASH
ARG NEXTAUTH_SECRET
ARG API_INTERNAL_URL
ARG API_PUBLIC_URL
ARG WEB_PUBLIC_URL

ENV GIT_HASH=${GIT_HASH}
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_OPTIONS=--max-old-space-size=1536
ENV AUTH_SECRET=${NEXTAUTH_SECRET}
ENV NEXTAUTH_SECRET=${NEXTAUTH_SECRET}
ENV NEXTAUTH_URL=${WEB_PUBLIC_URL}
ENV AUTH_TRUST_HOST=true
ENV API_URL=${API_INTERNAL_URL:-http://api:8080}
ENV NEXT_PUBLIC_API_URL=${API_PUBLIC_URL}
ENV NEXT_PUBLIC_APP_URL=${WEB_PUBLIC_URL}

RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo

COPY --from=deps /repo /repo
COPY . .

RUN rm -rf node_modules && pnpm install --frozen-lockfile --prefer-offline
RUN pnpm --filter @game-guild/client build && pnpm --filter @game-guild/web build

FROM node:${NODE_VERSION} AS runner
ARG APP_PORT

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=${APP_PORT}
ENV HOSTNAME=0.0.0.0

WORKDIR /app

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/static ./apps/web/.next/static
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/public ./apps/web/public

USER nextjs
EXPOSE ${APP_PORT}

CMD ["node", "apps/web/server.js"]
